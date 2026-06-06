using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Observability;

public sealed class LokiLogBrowserLogic
{
    private const int DefaultLimit = 200;
    private const int MaxLimit = 500;
    private static readonly TimeSpan DefaultLookback = TimeSpan.FromHours(6);
    private static readonly HttpClient SharedHttpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    private readonly HttpClient _httpClient;

    public LokiLogBrowserLogic(HttpClient httpClient = null)
    {
        _httpClient = httpClient ?? SharedHttpClient;
    }

    public async Task<List<string>> GetServiceNamesAsync(CancellationToken cancellationToken = default)
    {
        using JsonDocument payload = await GetJsonAsync("/loki/api/v1/label/service_name/values", cancellationToken);

        if (!payload.RootElement.TryGetProperty("data", out JsonElement data)
            || data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return data.EnumerateArray()
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<LokiLogQueryResponse> QueryAsync(
        string serviceName,
        string contains,
        int limit = DefaultLimit,
        string direction = "backward",
        DateTimeOffset? startUtc = null,
        DateTimeOffset? endUtc = null,
        CancellationToken cancellationToken = default)
    {
        string normalizedDirection = NormalizeDirection(direction);
        int normalizedLimit = Math.Clamp(limit, 1, MaxLimit);
        DateTimeOffset resolvedEndUtc = (endUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        DateTimeOffset resolvedStartUtc = (startUtc ?? resolvedEndUtc.Subtract(DefaultLookback)).ToUniversalTime();

        if (resolvedStartUtc > resolvedEndUtc)
            throw new ArgumentException("The requested start date must be earlier than the end date.", nameof(startUtc));

        string query = BuildQuery(serviceName, contains);
        string path = string.Create(CultureInfo.InvariantCulture, $"/loki/api/v1/query_range?query={Uri.EscapeDataString(query)}&limit={normalizedLimit}&direction={normalizedDirection}&start={ToUnixNanoseconds(resolvedStartUtc)}&end={ToUnixNanoseconds(resolvedEndUtc)}");

        using JsonDocument payload = await GetJsonAsync(path, cancellationToken);
        List<LokiLogEntry> entries = ParseEntries(payload.RootElement, normalizedDirection, normalizedLimit);

        return new LokiLogQueryResponse()
        {
            Query = query,
            ServiceName = string.IsNullOrWhiteSpace(serviceName) ? null : serviceName.Trim(),
            Contains = string.IsNullOrWhiteSpace(contains) ? null : contains.Trim(),
            StartUtc = resolvedStartUtc,
            EndUtc = resolvedEndUtc,
            Direction = normalizedDirection,
            Limit = normalizedLimit,
            Entries = entries
        };
    }

    private async Task<JsonDocument> GetJsonAsync(string relativePath, CancellationToken cancellationToken)
    {
        List<string> failures = new List<string>();

        foreach (Uri baseUri in ResolveBaseUris())
        {
            Uri requestUri = new Uri(baseUri, relativePath);

            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                string content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    failures.Add($"{requestUri}: {(int)response.StatusCode} {response.ReasonPhrase}");
                    continue;
                }

                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException exception)
            {
                failures.Add($"{requestUri}: {exception.Message}");
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                failures.Add($"{requestUri}: {exception.Message}");
            }
            catch (JsonException exception)
            {
                throw new LogsBackendUnavailableException($"Loki returned one invalid response payload. {exception.Message}");
            }
        }

        throw new LogsBackendUnavailableException($"Loki could not be reached. {string.Join(" | ", failures)}");
    }

    private static List<LokiLogEntry> ParseEntries(JsonElement root, string direction, int limit)
    {
        List<LokiLogEntry> entries = new List<LokiLogEntry>();

        if (!root.TryGetProperty("data", out JsonElement data)
            || !data.TryGetProperty("result", out JsonElement result)
            || result.ValueKind != JsonValueKind.Array)
        {
            return entries;
        }

        foreach (JsonElement streamResult in result.EnumerateArray())
        {
            Dictionary<string, string> labels = ParseLabels(streamResult);
            if (!streamResult.TryGetProperty("values", out JsonElement values)
                || values.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (JsonElement value in values.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < 2)
                    continue;

                string timestampValue = value[0].GetString();
                string message = value[1].GetString() ?? string.Empty;
                if (!TryParseUnixNanoseconds(timestampValue, out DateTimeOffset timestampUtc))
                    continue;

                entries.Add(new LokiLogEntry()
                {
                    TimestampUtc = timestampUtc,
                    Message = message,
                    Labels = new Dictionary<string, string>(labels, StringComparer.OrdinalIgnoreCase)
                });
            }
        }

        IEnumerable<LokiLogEntry> orderedEntries = string.Equals(direction, "forward", StringComparison.OrdinalIgnoreCase)
            ? entries.OrderBy(entry => entry.TimestampUtc)
            : entries.OrderByDescending(entry => entry.TimestampUtc);

        return orderedEntries.Take(limit).ToList();
    }

    private static Dictionary<string, string> ParseLabels(JsonElement streamResult)
    {
        Dictionary<string, string> labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!streamResult.TryGetProperty("stream", out JsonElement stream)
            || stream.ValueKind != JsonValueKind.Object)
        {
            return labels;
        }

        foreach (JsonProperty property in stream.EnumerateObject())
        {
            labels[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return labels;
    }

    private static IEnumerable<Uri> ResolveBaseUris()
    {
        HashSet<string> distinctUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string candidate in new[]
        {
            Environment.GetEnvironmentVariable("MANOIR_LOGS_LOKI_URL"),
            Environment.GetEnvironmentVariable("MANOIR_LOKI_URL"),
            Environment.GetEnvironmentVariable("MANOIR_OTEL_LOGS_ENDPOINT"),
            "http://loki:3100",
            "http://localhost:3100"
        })
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            string normalizedCandidate = NormalizeBaseUri(candidate);
            if (!distinctUris.Add(normalizedCandidate))
                continue;

            yield return new Uri(normalizedCandidate, UriKind.Absolute);
        }
    }

    private static string NormalizeBaseUri(string candidate)
    {
        string trimmed = candidate.Trim();

        if (trimmed.Contains("/otlp", StringComparison.OrdinalIgnoreCase))
        {
            int otlpIndex = trimmed.IndexOf("/otlp", StringComparison.OrdinalIgnoreCase);
            trimmed = trimmed[..otlpIndex];
        }

        if (trimmed.EndsWith("/", StringComparison.Ordinal))
            trimmed = trimmed[..^1];

        return trimmed;
    }

    private static string NormalizeDirection(string direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return "backward";

        string normalized = direction.Trim().ToLowerInvariant();
        if (normalized is "backward" or "forward")
            return normalized;

        throw new ArgumentException("The requested logs direction must be 'backward' or 'forward'.", nameof(direction));
    }

    private static string BuildQuery(string serviceName, string contains)
    {
        string selector = string.IsNullOrWhiteSpace(serviceName)
            ? "{service_name=~\".+\"}"
            : $"{{service_name=\"{EscapeLokiString(serviceName.Trim())}\"}}";

        return string.IsNullOrWhiteSpace(contains)
            ? selector
            : $"{selector} |~ \"(?i){EscapeLokiRegexLiteral(contains.Trim())}\"";
    }

    private static string EscapeLokiString(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string EscapeLokiRegexLiteral(string value)
    {
        return EscapeLokiString(Regex.Escape(value));
    }

    private static string ToUnixNanoseconds(DateTimeOffset value)
    {
        return (value.ToUnixTimeMilliseconds() * 1_000_000L).ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseUnixNanoseconds(string value, out DateTimeOffset timestampUtc)
    {
        timestampUtc = default;
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long nanoseconds))
            return false;

        long milliseconds = nanoseconds / 1_000_000L;
        timestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        return true;
    }
}