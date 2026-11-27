using System;

namespace MaNoir.Core.Contributions;

/// <summary>
/// Handles the public base path under which a plugin's Admin UI is deployed, such as "/home-automation".
/// </summary>
internal static class AdminUiPublicPath
{
    /// <summary>
    /// The environment variable through which Gaia tells an Admin UI where it is published.
    /// </summary>
    internal const string EnvironmentVariableName = "MANOIR_ADMINUI_PUBLIC_BASE_PATH";

    /// <summary>
    /// Returns the base path with one leading slash and no trailing one, or <see langword="null"/> for the site root.
    /// </summary>
    internal static string Normalize(string publicBasePath)
    {
        string trimmedPath = publicBasePath?.Trim().Trim('/');
        return string.IsNullOrEmpty(trimmedPath) ? null : "/" + trimmedPath;
    }

    /// <summary>
    /// Places a rooted page path under the base path. A path that already starts with it is returned unchanged.
    /// </summary>
    internal static string Compose(string publicBasePath, string pagePath)
    {
        string basePath = Normalize(publicBasePath);
        if (basePath == null
            || string.Equals(pagePath, basePath, StringComparison.OrdinalIgnoreCase)
            || pagePath.StartsWith(basePath + "/", StringComparison.OrdinalIgnoreCase))
        {
            return pagePath;
        }

        return basePath + pagePath;
    }
}
