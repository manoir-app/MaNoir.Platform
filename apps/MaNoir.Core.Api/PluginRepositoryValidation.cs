using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Api;

public sealed class PluginRepositoryValidation
{
    public string RepositoryUrl { get; init; }

    public string Status { get; init; }

    public string Provider { get; init; }

    public bool Exists { get; init; }

    public bool IsOfficial { get; init; }

    public bool IsSupported { get; init; }

    public bool CanInstall { get; init; }

    public string Message { get; init; }
}

public sealed class PluginRepositoryValidator
{
    public const string HttpClientName = "plugin-repository-validation";
    private const string OfficialGitHubOwner = "manoir-app";

    private readonly IHttpClientFactory _httpClientFactory;

    public PluginRepositoryValidator(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<PluginRepositoryValidation> ValidateAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return Create(repositoryUrl, "missing", null, "Repository URL is required.");
        }

        if (!Uri.TryCreate(repositoryUrl.Trim(), UriKind.Absolute, out Uri uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(uri.UserInfo)
            || !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            return Create(repositoryUrl, "unsupported", null, "Only public HTTPS GitHub and GitLab repositories are supported.");
        }

        string host = uri.Host.Trim().ToLowerInvariant();
        string[] pathSegments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (host is "github.com" or "www.github.com")
        {
            if (pathSegments.Length != 2)
                return Create(uri.AbsoluteUri, "unsupported", "github", "The GitHub repository URL must identify an owner and a repository.");

            string owner = pathSegments[0];
            string repository = TrimGitSuffix(pathSegments[1]);
            return await CheckRemoteRepositoryAsync(
                uri.AbsoluteUri,
                $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}",
                "github",
                string.Equals(owner, OfficialGitHubOwner, StringComparison.OrdinalIgnoreCase),
                cancellationToken);
        }

        if (host is "gitlab.com" or "www.gitlab.com")
        {
            if (pathSegments.Length < 2)
                return Create(uri.AbsoluteUri, "unsupported", "gitlab", "The GitLab repository URL must identify a group and a repository.");

            string projectPath = string.Join('/', pathSegments);
            projectPath = TrimGitSuffix(projectPath);
            return await CheckRemoteRepositoryAsync(
                uri.AbsoluteUri,
                $"https://gitlab.com/api/v4/projects/{Uri.EscapeDataString(projectPath)}",
                "gitlab",
                false,
                cancellationToken);
        }

        return Create(uri.AbsoluteUri, "unsupported", null, "This repository host is not supported. Installation is at the user's responsibility.");
    }

    private async Task<PluginRepositoryValidation> CheckRemoteRepositoryAsync(
        string repositoryUrl,
        string apiUrl,
        string provider,
        bool isOfficial,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            using HttpResponseMessage response = await _httpClientFactory
                .CreateClient(HttpClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if ((int)response.StatusCode == 404)
            {
                return Create(repositoryUrl, "notFound", provider, "The repository could not be found.", isOfficial);
            }

            if (!response.IsSuccessStatusCode)
            {
                return Create(repositoryUrl, "unavailable", provider, "The repository could not be verified right now.", isOfficial);
            }

            return Create(repositoryUrl, isOfficial ? "official" : "nonOfficial", provider, null, isOfficial, true);
        }
        catch (HttpRequestException)
        {
            return Create(repositoryUrl, "unavailable", provider, "The repository could not be verified right now.", isOfficial);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Create(repositoryUrl, "unavailable", provider, "The repository verification timed out.", isOfficial);
        }
    }

    private static PluginRepositoryValidation Create(
        string repositoryUrl,
        string status,
        string provider,
        string message,
        bool isOfficial = false,
        bool exists = false)
    {
        bool isSupported = status is "official" or "nonOfficial";
        return new PluginRepositoryValidation()
        {
            RepositoryUrl = repositoryUrl?.Trim(),
            Status = status,
            Provider = provider,
            Exists = exists,
            IsOfficial = isOfficial && exists,
            IsSupported = isSupported,
            CanInstall = exists && isSupported,
            Message = message
        };
    }

    private static string TrimGitSuffix(string value)
    {
        return value.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }
}