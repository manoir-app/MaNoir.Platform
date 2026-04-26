using MaNoir.Core.Contributions;
using MaNoir.Core.Contracts.Models.Contributions;
using Microsoft.AspNetCore.Builder;

namespace MaNoir.Core.Api;

/// <summary>
/// Exposes startup helpers for published plugin catalogs.
/// </summary>
public static class CoreApiAuthorizationCatalogModule
{
    /// <summary>
    /// Publishes one complete plugin catalog during application startup.
    /// </summary>
    public static WebApplication RegisterPlugin(this WebApplication app, PluginDescriptor pluginDescriptor)
    {
        new PluginRegistrationLogic().PublishPluginDescriptorAsync(pluginDescriptor).GetAwaiter().GetResult();
        return app;
    }
}