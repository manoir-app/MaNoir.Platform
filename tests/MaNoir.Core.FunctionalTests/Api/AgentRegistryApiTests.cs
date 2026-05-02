using MaNoir.Core.Agents;
using MaNoir.Core.Api;
using MaNoir.Core.Contracts.Models.Agents;
using MaNoir.Core.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class AgentRegistryApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task RegisterAndHeartbeat_ShouldPersistAgentStateThroughSharedLogic()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope apiKeyScope = new("MANOIR_APIKEY", "tests-agents-apikey");

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "tests-agents-apikey");

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/core/system/agents/register", new AgentRegistrationRequest()
        {
            AgentId = "erza",
            DisplayName = "Erza",
            MeshId = "local",
            Version = "1.2.3",
            Capabilities = ["presence", "mesh.monitoring"],
            State = AgentState.Starting,
            StatusMessage = "Starting"
        });
        RegisteredAgent registeredAgent = await registerResponse.Content.ReadFromJsonAsync<RegisteredAgent>();

        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.IsNotNull(registeredAgent);
        Assert.AreEqual("local:erza", registeredAgent.Id);
        Assert.AreEqual(AgentState.Starting, registeredAgent.State);
        CollectionAssert.AreEqual(new List<string>() { "mesh.monitoring", "presence" }, registeredAgent.Capabilities);

        HttpResponseMessage heartbeatResponse = await client.PostAsJsonAsync("/api/core/system/agents/heartbeat", new AgentHeartbeatRequest()
        {
            AgentId = "erza",
            MeshId = "local",
            State = AgentState.Ready,
            StatusMessage = "Running"
        });
        RegisteredAgent heartbeatAgent = await heartbeatResponse.Content.ReadFromJsonAsync<RegisteredAgent>();

        Assert.AreEqual(HttpStatusCode.OK, heartbeatResponse.StatusCode);
        Assert.IsNotNull(heartbeatAgent);
        Assert.AreEqual(AgentState.Ready, heartbeatAgent.State);
        Assert.AreEqual("Running", heartbeatAgent.StatusMessage);

        RegisteredAgent storedAgent = await new AgentRegistryLogic().GetAgentAsync("local", "erza");

        Assert.IsNotNull(storedAgent);
        Assert.AreEqual("Erza", storedAgent.DisplayName);
        Assert.AreEqual("1.2.3", storedAgent.Version);
        Assert.AreEqual(AgentState.Ready, storedAgent.State);
        Assert.AreEqual("Running", storedAgent.StatusMessage);
        Assert.IsTrue(storedAgent.LastHeartbeatUtc >= storedAgent.RegisteredAtUtc);
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