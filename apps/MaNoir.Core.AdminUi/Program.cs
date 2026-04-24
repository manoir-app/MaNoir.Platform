using MaNoir.Core.AdminUi.Hosting;
using MaNoir.Core.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddMaNoirCoreAdminUiHosting();
builder.Services.AddMaNoirCoreApi();

var app = builder.Build();

app.MapMaNoirCoreApi();

app.Run();
