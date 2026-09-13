namespace MaNoir.Core.AdminUi.Hosting;

internal static class AdminUiHostingRewrite
{
    internal static string ResolveRouterBasePath(string routerBasePath, string publicBasePath)
    {
        string normalizedRouterBasePath = NormalizeRouterBasePath(routerBasePath);
        string normalizedPublicBasePath = NormalizePublicBasePath(publicBasePath);

        return normalizedRouterBasePath == "/" && normalizedPublicBasePath != null
            ? NormalizeRouterBasePath(normalizedPublicBasePath)
            : normalizedRouterBasePath;
    }

    internal static string RewriteRootSpaAssetReferences(string indexHtml, string assetPrefix)
    {
        return indexHtml
            .Replace("src=\"/", $"src=\"{assetPrefix}")
            .Replace("href=\"/", $"href=\"{assetPrefix}")
            .Replace("src='/", $"src='{assetPrefix}")
            .Replace("href='/", $"href='{assetPrefix}")
            .Replace("src=\"./", $"src=\"{assetPrefix}")
            .Replace("href=\"./", $"href=\"{assetPrefix}")
            .Replace("src='./", $"src='{assetPrefix}")
            .Replace("href='./", $"href='{assetPrefix}");
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

        return trimmedPath.TrimEnd('/') + "/";
    }
}