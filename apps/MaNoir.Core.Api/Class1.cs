using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MaNoir.Core.Api;

public static class CoreApiModule
{
	public static IServiceCollection AddMaNoirCoreApi(this IServiceCollection services)
	{
		return services;
	}

	public static IEndpointRouteBuilder MapMaNoirCoreApi(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/api/core");
		group.MapGet("/health", () => Results.Ok(new { status = "ok" }));

		return endpoints;
	}
}
