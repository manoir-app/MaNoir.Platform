using MaNoir.Core.Api;
using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.DataAccess;
using MaNoir.Core.Contributions;
using MaNoir.Core.Contracts.Models.Setup;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class InitialSetupApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task Initialize_ShouldCreateTheLocalMeshAndTheMasterAdminWhenTheDatabaseIsEmpty()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        InitialSetupStatus initialStatus = await client.GetFromJsonAsync<InitialSetupStatus>("/api/core/setup/status");
        HttpResponseMessage initializeResponse = await client.PostAsJsonAsync("/api/core/setup/initialize", new InitialSetupRequest()
        {
            AdminUserId = "sarah",
            AdminFirstName = "Sarah",
            AdminName = "Connor",
            AdminCommonName = "Sarah",
            AdminEmail = "sarah@manoir.app",
            AdminPassword = "P@ssword-42",
            LanguageId = "fr-FR",
            TimeZoneId = "Europe/Paris",
            CountryId = "FR"
        });
        InitialSetupResponse payload = await initializeResponse.Content.ReadFromJsonAsync<InitialSetupResponse>();
        InitialSetupStatus afterStatus = await client.GetFromJsonAsync<InitialSetupStatus>("/api/core/setup/status");
        UserAuthenticationResponse loginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();
        ContributionLogic contributionLogic = new ContributionLogic();
        InstalledPlugin corePlugin = await contributionLogic.GetInstalledPluginAsync("core");
        ContributionDefinition coreAdminContribution = (await contributionLogic.GetContributionDefinitionsAsync("core"))
            .Single(definition => definition.Kind == ContributionKind.AdminUiPage);

        Assert.IsNotNull(initialStatus);
        Assert.IsTrue(initialStatus.CanInitialize);
        Assert.IsFalse(initialStatus.HasMesh);
        Assert.IsFalse(initialStatus.HasUsers);

        Assert.AreEqual(HttpStatusCode.OK, initializeResponse.StatusCode);
        Assert.IsNotNull(payload);
        Assert.AreEqual("local", payload.Mesh.Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.Mesh.PublicId));
        Assert.IsNotNull(payload.Mesh.MainServer);
        Assert.AreEqual("fr-FR", payload.Mesh.LanguageId);
        Assert.AreEqual("Europe/Paris", payload.Mesh.TimeZoneId);
        Assert.AreEqual("FR", payload.Mesh.CountryId);
        Assert.AreEqual("sarah", payload.User.Id);
        Assert.IsTrue(payload.User.IsAdmin);
        Assert.IsTrue(payload.User.IsMain);
        Assert.IsNull(payload.User.HashedPassword);

        Assert.IsNotNull(afterStatus);
        Assert.IsFalse(afterStatus.CanInitialize);
        Assert.IsTrue(afterStatus.HasMesh);
        Assert.IsTrue(afterStatus.HasUsers);

        Assert.IsNotNull(loginPayload);
        Assert.IsFalse(string.IsNullOrWhiteSpace(loginPayload.AccessToken));
        Assert.IsNotNull(corePlugin);
        Assert.AreEqual("core", corePlugin.Id);
        Assert.IsNotNull(coreAdminContribution.AdminUi);
        Assert.AreEqual(CoreAccessZones.CoreAdminUi, coreAdminContribution.AdminUi.AccessZoneId);
        Assert.AreEqual(AccessLevel.Read, coreAdminContribution.AdminUi.RequiredAccessLevel);
        Assert.AreEqual("/admin/core", coreAdminContribution.AdminUi.Pages.Single().Url);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Initialize_ShouldReturnConflictProblemDetailsWhenTheInstanceIsAlreadyInitialized()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        AutomationMeshLogic meshLogic = new AutomationMeshLogic();
        await meshLogic.SaveAsync(AutomationMeshLogic.CreateLocalMesh("machine-a", "https://localhost:5001"));

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage initializeResponse = await client.PostAsJsonAsync("/api/core/setup/initialize", new InitialSetupRequest()
        {
            AdminUserId = "sarah",
            AdminPassword = "P@ssword-42"
        });
        ProblemDetails problem = await initializeResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.AreEqual(HttpStatusCode.Conflict, initializeResponse.StatusCode);
        Assert.IsNotNull(problem);
        Assert.AreEqual(409, problem.Status);
        Assert.AreEqual("https://manoir.app/problems/setup/initialization-unavailable", problem.Type);
        Assert.AreEqual("Initial setup is no longer available", problem.Title);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Status_ShouldStayUnavailableFromCacheOnceInitializationCompleted()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage initializeResponse = await client.PostAsJsonAsync("/api/core/setup/initialize", new InitialSetupRequest()
        {
            AdminUserId = "sarah",
            AdminPassword = "P@ssword-42"
        });

        Assert.AreEqual(HttpStatusCode.OK, initializeResponse.StatusCode);

        UserMongoOperations userMongoOperations = new UserMongoOperations();
        AutomationMeshLogic meshLogic = new AutomationMeshLogic();
        await userMongoOperations.DeleteAsync("sarah");
        await meshLogic.DeleteLocalAsync();

        InitialSetupStatus cachedStatus = await client.GetFromJsonAsync<InitialSetupStatus>("/api/core/setup/status");

        Assert.IsNotNull(cachedStatus);
        Assert.IsFalse(cachedStatus.CanInitialize);
        Assert.IsTrue(cachedStatus.HasMesh);
        Assert.IsTrue(cachedStatus.HasUsers);
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