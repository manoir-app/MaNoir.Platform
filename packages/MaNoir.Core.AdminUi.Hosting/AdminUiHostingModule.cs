using Microsoft.AspNetCore.Builder;

namespace MaNoir.Core.AdminUi.Hosting;

/// <summary>
/// Exposes the bootstrap extensions of the Core Admin UI hosting package.
/// </summary>
public static class AdminUiHostingModule
{
    /// <summary>
    /// Adds the Core Admin UI hosting services and conventions to the target application builder.
    /// </summary>
    /// <param name="builder">Application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static WebApplicationBuilder AddMaNoirCoreAdminUiHosting(this WebApplicationBuilder builder)
    {
        return builder;
    }
}