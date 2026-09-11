using MaNoir.Core.AdminUi.Hosting;
using MaNoir.Core.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MaNoir.Core.AdminUi;

public static class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.AddMaNoirCoreAdminUiHosting();
		builder.AddMaNoirCoreApi();

		WebApplication app = builder.Build();

		app.UseMaNoirCoreApi();
		app.UseMaNoirCoreAdminUiHosting();

		app.Run();
	}
}
