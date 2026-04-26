using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MaNoir.Core.Contributions;

internal static partial class ContributionSecretReferenceHelper
{
    private const string Prefix = "SECRET:";

    [GeneratedRegex("^\\{\\{\\s*SECRET\\s*:\\s*(?<secretId>.+?)\\s*\\}\\}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretReferenceRegex();

    public static bool ContainsSecretReferences(IReadOnlyDictionary<string, string> settings)
    {
        return GetReferencedSecretIds(settings).Count > 0;
    }

    public static HashSet<string> GetReferencedSecretIds(IReadOnlyDictionary<string, string> settings)
    {
        HashSet<string> referencedSecretIds = new(StringComparer.OrdinalIgnoreCase);
        if (settings == null || settings.Count == 0)
            return referencedSecretIds;

        foreach (string value in settings.Values.Where(value => value != null))
        {
            string secretId = TryGetReferencedSecretId(value);
            if (secretId != null)
                referencedSecretIds.Add(secretId);
        }

        return referencedSecretIds;
    }

    public static string TryGetReferencedSecretId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        Match match = SecretReferenceRegex().Match(value.Trim());
        if (!match.Success)
            return null;

        string secretId = match.Groups["secretId"].Value.Trim();
        return string.IsNullOrWhiteSpace(secretId) ? null : secretId.ToLowerInvariant();
    }
}