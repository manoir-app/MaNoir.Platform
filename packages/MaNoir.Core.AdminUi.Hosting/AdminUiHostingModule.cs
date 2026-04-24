using Microsoft.AspNetCore.Builder;

namespace MaNoir.Core.AdminUi.Hosting;

public static class AdminUiHostingModule
{
    public static WebApplicationBuilder AddMaNoirCoreAdminUiHosting(this WebApplicationBuilder builder)
    {
        return builder;
    }
}