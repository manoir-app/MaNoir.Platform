using MaNoir.Core.AdminUi.Hosting;
using MaNoir.Core.Api;
using MaNoir.Core.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MaNoir.Core.AdminUi;

public static class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.AddMaNoirAdminUiHosting(options =>
		{
			options.SpaFolders = new[] { "bootstrap", "front" };
			options.DefaultSpaFolderResolver = async cancellationToken =>
			{
				var status = await new InitialSetupLogic().GetStatusAsync(cancellationToken);
				return status?.CanInitialize == true ? "bootstrap" : "front";
			};
		});
		builder.AddMaNoirCoreApi();

		WebApplication app = builder.Build();

		app.UseMaNoirAdminUiHosting();
		app.UseRouting();
		app.UseMaNoirCoreApi();

		app.Run();
	}
}
