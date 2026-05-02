using MaNoir.Core.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

    /// <summary>
    /// Adds the Core Admin UI hosting services and conventions to the target application builder.
    /// </summary>
    /// <param name="builder">Application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static WebApplicationBuilder AddMaNoirCoreAdminUiHosting(this WebApplicationBuilder builder)
    {
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
            await SendSpaIndexAsync(app.Environment, context, BootstrapSpaFolder);
            return;
        }

        if (requestPath.StartsWithSegments("/front", StringComparison.OrdinalIgnoreCase, out PathString frontRemainder)
            && !HasFileExtension(frontRemainder))
        {
            await SendSpaIndexAsync(app.Environment, context, FrontSpaFolder);
            return;
        }

        await SendSpaIndexAsync(app.Environment, context, await ResolveDefaultSpaFolderAsync(context.RequestAborted));
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

    private static async Task SendSpaIndexAsync(IHostEnvironment environment, HttpContext context, string spaFolder)
    {
        string webRootPath = environment.ContentRootPath;
        string candidateFile = Path.Combine(webRootPath, "wwwroot", spaFolder, "index.html");
        if (!File.Exists(candidateFile))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"The Admin UI bundle '{spaFolder}' is not available.");
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(candidateFile, context.RequestAborted);
    }
}