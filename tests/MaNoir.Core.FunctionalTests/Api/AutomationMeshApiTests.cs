using MaNoir.Core.Api;
using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class AutomationMeshApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetLocalFrontendUrls_ShouldExposeMeshFrontendCatalog()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        AutomationMeshLogic logic = new AutomationMeshLogic();
        await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");
        await logic.SetFrontendUrlAsync("adminui", "https://admin.demo.manoir.app/");

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/api/core/system/mesh/local/frontends");
        Dictionary<string, string> frontendUrls = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(frontendUrls);
        Assert.AreEqual("https://localhost:5001/", frontendUrls["home"]);
        Assert.AreEqual("https://admin.demo.manoir.app/", frontendUrls["adminui"]);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task WriteLocalFrontendUrls_ShouldRequireMeshManageAccessAndPersistChanges()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        AutomationMeshLogic logic = new AutomationMeshLogic();
        await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");

        await SeedUsersAsync();
        await new AuthorizationLogic().ReplaceUserAuthorizationAsync("claire",
        [
            new UserZoneAccess() { ZoneId = CoreAccessZones.CoreMeshSettings, Level = AccessLevel.Manage }
        ]);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string johnToken = await LoginAsync(client, "john");
        string claireToken = await LoginAsync(client, "claire");

        HttpResponseMessage unauthorizedResponse = await client.PutAsJsonAsync("/api/core/system/mesh/local/frontends/adminui", new AutomationMeshFrontendUrlUpsertRequest()
        {
            Url = "https://admin.demo.manoir.app/"
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

        using HttpRequestMessage forbiddenRequest = new(HttpMethod.Put, "/api/core/system/mesh/local/frontends/adminui")
        {
            Content = JsonContent.Create(new AutomationMeshFrontendUrlUpsertRequest()
            {
                Url = "https://admin.demo.manoir.app/"
            })
        };
        forbiddenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage forbiddenResponse = await client.SendAsync(forbiddenRequest);

        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using HttpRequestMessage putRequest = new(HttpMethod.Put, "/api/core/system/mesh/local/frontends/adminui")
        {
            Content = JsonContent.Create(new AutomationMeshFrontendUrlUpsertRequest()
            {
                Url = "https://admin.demo.manoir.app/"
            })
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claireToken);

        HttpResponseMessage putResponse = await client.SendAsync(putRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, putResponse.StatusCode);
        Assert.AreEqual("https://admin.demo.manoir.app/", await logic.GetFrontendUrlAsync("adminui"));

        using HttpRequestMessage deleteRequest = new(HttpMethod.Delete, "/api/core/system/mesh/local/frontends/adminui");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claireToken);

        HttpResponseMessage deleteResponse = await client.SendAsync(deleteRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.IsNull(await logic.GetFrontendUrlAsync("adminui"));
    }

    private static async Task SeedUsersAsync()
    {
        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "john",
            FirstName = "John",
            Name = "Doe",
            CommonName = "John",
            IsAdmin = false,
            IsMain = true,
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("john", "P@ssword-42");
        await userLogic.SaveAsync(new User()
        {
            Id = "claire",
            FirstName = "Claire",
            Name = "Fisher",
            CommonName = "Claire",
            IsAdmin = false,
            IsMain = true,
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("claire", "P@ssword-42");
    }

    private static async Task<string> LoginAsync(HttpClient client, string userId)
    {
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = userId,
            Password = "P@ssword-42"
        });
        UserAuthenticationResponse payload = await loginResponse.Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload?.AccessToken));
        return payload.AccessToken;
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