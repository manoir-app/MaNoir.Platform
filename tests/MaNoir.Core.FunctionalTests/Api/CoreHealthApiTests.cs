using MaNoir.Core.Api;
using MaNoir.Core.Contracts.Models.Health;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class CoreHealthApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetServerInfo_ShouldExposeMeshNameAdminUiVersionAndUptime()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        AutomationMeshLogic logic = new AutomationMeshLogic();
        AutomationMesh mesh = await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");
        AutomationMeshLogic.ApplyPublicBaseDomain(mesh, "maison.local");
        await logic.SaveAsync(mesh);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/api/core/health/server-info");
        CoreServerHealthInfo payload = await response.Content.ReadFromJsonAsync<CoreServerHealthInfo>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(payload);
        Assert.AreEqual("machine-a", payload.MeshName);
        Assert.AreEqual("maison.local", payload.DomainName);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.AdminUiVersion));
        Assert.IsTrue(payload.UptimeSeconds >= 0);
        Assert.IsTrue(payload.StartedAtUtc <= DateTimeOffset.UtcNow);
    }

    private static WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>()
        {
            ["MaNoir:Authentication:UsersJwt:Issuer"] = "tests.manoir.core",
            ["MaNoir:Authentication:UsersJwt:Audience"] = "tests.manoir.core.users",
            ["MaNoir:Authentication:UsersJwt:SigningKey"] = "tests-only-signing-key-value-with-more-than-32-chars",
            ["MaNoir:Authentication:UsersJwt:CookieName"] = "manoir_test_users_access_token",
            ["MaNoir:Authentication:UsersJwt:AccessTokenLifetimeMinutes"] = "120"
        });
        builder.AddMaNoirCoreApi();

        WebApplication app = builder.Build();
        app.UseMaNoirCoreApi();
        return app;
    }
}