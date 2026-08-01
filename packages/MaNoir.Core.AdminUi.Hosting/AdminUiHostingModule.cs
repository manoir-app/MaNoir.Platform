using MaNoir.Core.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MaNoir.Core.AdminUi.Hosting;

/// <summary>
/// Exposes the hosting extensions of the Core Admin UI package.
/// </summary>
public static class AdminUiHostingModule
{
    private const string BootstrapSpaFolder = "bootstrap";
    private const string FrontSpaFolder = "front";
    private const string PublicBasePathItemKey = "MaNoir.AdminUi.PublicBasePath";

    /// <summary>
    /// Adds the Core Admin UI hosting services and conventions to the target application builder.
    /// </summary>
    /// <param name="builder">Application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static WebApplicationBuilder AddMaNoirCoreAdminUiHosting(this WebApplicationBuilder builder)
    {
        AdminUiHostingOptions options = new AdminUiHostingOptions();
        builder.Configuration.GetSection("MaNoir:AdminUi:Hosting").Bind(options);
        options.PublicBasePath ??= Environment.GetEnvironmentVariable("MANOIR_ADMINUI_PUBLIC_BASE_PATH");

        builder.Services.AddSingleton(options);
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>
    /// Enables static file hosting and SPA fallback resolution for the Core Admin UI frontends.
    /// </summary>
    /// <param name="app">Application pipeline to configure.</param>
    /// <returns>The same <paramref name="app"/> instance for chaining.</returns>
    public static WebApplication UseMaNoirCoreAdminUiHosting(this WebApplication app)
    {
        AdminUiHostingOptions options = app.Services.GetRequiredService<AdminUiHostingOptions>();

        app.Use(async (context, next) =>
        {
            string publicBasePath = NormalizePublicBasePath(options.PublicBasePath);
            PathString requestPath = context.Request.Path;

            if (!string.IsNullOrWhiteSpace(publicBasePath)
                && requestPath.StartsWithSegments(publicBasePath, StringComparison.OrdinalIgnoreCase, out PathString remainder))
            {
                context.Items[PublicBasePathItemKey] = publicBasePath;
                context.Request.Path = remainder.HasValue ? remainder : new PathString("/");
            }

            if (ShouldRemapRootStaticAssetRequest(context.Request.Path)
                && !RootStaticFileExists(app.Environment, context.Request.Path))
            {
                string spaFolder = await ResolveDefaultSpaFolderAsync(context.RequestAborted);
                context.Request.Path = new PathString($"/{spaFolder}{context.Request.Path}");
            }

            await next();
        });

        app.UseStaticFiles();
        app.MapHealthChecks("/healthz");

        app.MapFallback(context => HandleSpaFallbackAsync(app, context));
        return app;
    }

    private static async Task HandleSpaFallbackAsync(WebApplication app, HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        PathString requestPath = context.Request.Path;
        if (requestPath.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) || HasFileExtension(requestPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (requestPath.StartsWithSegments("/bootstrap", StringComparison.OrdinalIgnoreCase, out PathString bootstrapRemainder)
            && !HasFileExtension(bootstrapRemainder))
        {
            await SendSpaIndexAsync(app.Environment, context, BootstrapSpaFolder, remapToRoot: false);
            return;
        }

        if (requestPath.StartsWithSegments("/front", StringComparison.OrdinalIgnoreCase, out PathString frontRemainder)
            && !HasFileExtension(frontRemainder))
        {
            await SendSpaIndexAsync(app.Environment, context, FrontSpaFolder, remapToRoot: false);
            return;
        }

        await SendSpaIndexAsync(app.Environment, context, await ResolveDefaultSpaFolderAsync(context.RequestAborted), remapToRoot: true);
    }

    private static bool ShouldRemapRootStaticAssetRequest(PathString path)
    {
        if (!HasFileExtension(path))
        {
            return false;
        }

        return !path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments("/bootstrap", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments("/front", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasFileExtension(PathString path)
    {
        return !string.IsNullOrWhiteSpace(path.Value) && Path.HasExtension(path.Value);
    }

    private static async Task<string> ResolveDefaultSpaFolderAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var status = await new InitialSetupLogic().GetStatusAsync(cancellationToken);
            return status?.CanInitialize == true ? BootstrapSpaFolder : FrontSpaFolder;
        }
        catch
        {
            return BootstrapSpaFolder;
        }
    }

    private static bool RootStaticFileExists(IHostEnvironment environment, PathString requestPath)
    {
        string candidateFile = GetWebRootFilePath(environment, requestPath.Value);
        return File.Exists(candidateFile);
    }

    private static async Task SendSpaIndexAsync(IHostEnvironment environment, HttpContext context, string spaFolder, bool remapToRoot)
    {
        string candidateFile = GetWebRootFilePath(environment, $"/{spaFolder}/index.html");
        if (!File.Exists(candidateFile))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"The Admin UI bundle '{spaFolder}' is not available.");
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }

        if (!remapToRoot)
        {
            string prefixedIndexHtml = await File.ReadAllTextAsync(candidateFile, context.RequestAborted);
            prefixedIndexHtml = RewriteSpaIndex(prefixedIndexHtml, spaFolder, ResolveRequestPublicBasePath(context), string.Concat("/", spaFolder));
            await context.Response.WriteAsync(prefixedIndexHtml, context.RequestAborted);
            return;
        }

        string indexHtml = await File.ReadAllTextAsync(candidateFile, context.RequestAborted);
        indexHtml = RewriteSpaIndex(indexHtml, spaFolder, ResolveRequestPublicBasePath(context), "/");
        await context.Response.WriteAsync(indexHtml, context.RequestAborted);
    }

    private static string GetWebRootFilePath(IHostEnvironment environment, string requestPath)
    {
        string relativePath = requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(environment.ContentRootPath, "wwwroot", relativePath);
    }

    private static string RewriteSpaIndex(string indexHtml, string spaFolder, string publicBasePath, string routerBasePath)
    {
        string normalizedPublicBasePath = NormalizePublicBasePath(publicBasePath);
        string assetPrefix = string.IsNullOrWhiteSpace(normalizedPublicBasePath)
            ? $"/{spaFolder}/"
            : $"{normalizedPublicBasePath}/{spaFolder}/";

        string rewrittenHtml = indexHtml
            .Replace($"\"/{spaFolder}/", $"\"{assetPrefix}")
            .Replace($"'/{spaFolder}/", $"'{assetPrefix}");

        string normalizedRouterBasePath = NormalizeRouterBasePath(routerBasePath);
        string runtimeScript = $"<script>window.__MANOIR_ADMIN_UI_CONFIG__={{routerBasePath:{System.Text.Json.JsonSerializer.Serialize(normalizedRouterBasePath)},publicBasePath:{System.Text.Json.JsonSerializer.Serialize(normalizedPublicBasePath)}}};</script>";
        return rewrittenHtml.Replace("<head>", $"<head>{runtimeScript}");
    }

    private static string ResolveRequestPublicBasePath(HttpContext context)
    {
        if (context.Items.TryGetValue(PublicBasePathItemKey, out object publicBasePath)
            && publicBasePath is string stringValue
            && !string.IsNullOrWhiteSpace(stringValue))
        {
            return stringValue;
        }

        return null;
    }

    private static string NormalizePublicBasePath(string publicBasePath)
    {
        if (string.IsNullOrWhiteSpace(publicBasePath))
            return null;

        string trimmedPath = publicBasePath.Trim();
        if (!trimmedPath.StartsWith("/", StringComparison.Ordinal))
            trimmedPath = "/" + trimmedPath;

        return trimmedPath.Length > 1 ? trimmedPath.TrimEnd('/') : trimmedPath;
    }

    private static string NormalizeRouterBasePath(string routerBasePath)
    {
        if (string.IsNullOrWhiteSpace(routerBasePath) || string.Equals(routerBasePath, "/", StringComparison.Ordinal))
            return "/";

        string trimmedPath = routerBasePath.Trim();
        if (!trimmedPath.StartsWith("/", StringComparison.Ordinal))
            trimmedPath = "/" + trimmedPath;

        return trimmedPath.EndsWith("/", StringComparison.Ordinal) ? trimmedPath : trimmedPath + "/";
    }
}