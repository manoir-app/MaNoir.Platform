namespace MaNoir.Core.AdminUi.Hosting;

internal static class AdminUiHostingRewrite
{
    internal static string ResolveRouterBasePath(string routerBasePath, string publicBasePath)
    {
        string normalizedRouterBasePath = NormalizeRouterBasePath(routerBasePath);
        string normalizedPublicBasePath = NormalizePublicBasePath(publicBasePath);

        // React Router's basename must equal window.location.pathname exactly (no trailing slash),
        // so it always combines with the public prefix instead of dropping it.
        if (normalizedRouterBasePath == "/")
            return normalizedPublicBasePath ?? "/";

        return normalizedPublicBasePath == null
            ? normalizedRouterBasePath
            : normalizedPublicBasePath + normalizedRouterBasePath;
    }

    /// <summary>
    /// Resolves the single asset prefix used to build the SPA's &lt;base href&gt; tag, for the given SPA folder and public base path.
    /// </summary>
    internal static string ResolveAssetPrefix(string spaFolder, string publicBasePath)
    {
        string normalizedPublicBasePath = NormalizePublicBasePath(publicBasePath);
        string trimmedSpaFolder = string.IsNullOrWhiteSpace(spaFolder) ? null : spaFolder.Trim('/');

        if (string.IsNullOrWhiteSpace(trimmedSpaFolder))
            return string.IsNullOrWhiteSpace(normalizedPublicBasePath) ? "/" : $"{normalizedPublicBasePath}/";

        return string.IsNullOrWhiteSpace(normalizedPublicBasePath)
            ? $"/{trimmedSpaFolder}/"
            : $"{normalizedPublicBasePath}/{trimmedSpaFolder}/";
    }

    private static string NormalizePublicBasePath(string publicBasePath)
    {
        if (string.IsNullOrWhiteSpace(publicBasePath) || publicBasePath.Trim() == "/")
            return null;

        string trimmedPath = publicBasePath.Trim();
        if (!trimmedPath.StartsWith("/"))
            trimmedPath = "/" + trimmedPath;

        return trimmedPath.TrimEnd('/');
    }

    private static string NormalizeRouterBasePath(string routerBasePath)
    {
        if (string.IsNullOrWhiteSpace(routerBasePath) || routerBasePath.Trim() == "/")
            return "/";

        string trimmedPath = routerBasePath.Trim();
        if (!trimmedPath.StartsWith("/"))
            trimmedPath = "/" + trimmedPath;

        // No trailing slash: React Router rejects a basename that doesn't match location.pathname exactly.
        return trimmedPath.TrimEnd('/');
    }
}