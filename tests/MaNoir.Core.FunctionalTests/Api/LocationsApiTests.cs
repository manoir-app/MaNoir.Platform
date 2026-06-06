using MaNoir.Core.Api;
using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Locations;
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
public sealed class LocationsApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetLocations_ShouldExposePersistedStructureForAuthenticatedUsers()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await SeedUsersAsync();

        await new LocationLogic().UpsertAsync(new Location()
        {
            Id = "maison-principale",
            Name = "Maison principale",
            Zones =
            {
                new LocationZone()
                {
                    Name = "Rez-de-chaussee",
                    Rooms =
                    {
                        new LocationRoom()
                        {
                            Name = "Salon",
                            FloorLevel = 0
                        }
                    }
                }
            }
        });

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string token = await LoginAsync(client, "sarah");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/core/system/locations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);
        List<Location> payload = await response.Content.ReadFromJsonAsync<List<Location>>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(payload);
        Assert.AreEqual(1, payload.Count);
        Assert.AreEqual("maison-principale", payload[0].Id);
        Assert.AreEqual("Maison principale", payload[0].Name);
        Assert.AreEqual(1, payload[0].Zones.Count);
        Assert.AreEqual(1, payload[0].Zones[0].Rooms.Count);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PutLocation_ShouldRequireAuthenticationAndPersistNestedIdentifiers()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await SeedUsersAsync();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();

        HttpResponseMessage unauthorizedResponse = await client.PutAsJsonAsync("/api/core/system/locations/maison-annexe", new Location()
        {
            Name = "Maison annexe"
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

        string token = await LoginAsync(client, "sarah");

        using HttpRequestMessage request = new(HttpMethod.Put, "/api/core/system/locations/maison-annexe")
        {
            Content = JsonContent.Create(new Location()
            {
                Name = "Maison annexe",
                Zones =
                {
                    new LocationZone()
                    {
                        Name = "Niveau unique",
                        Rooms =
                        {
                            new LocationRoom()
                            {
                                Name = "Atelier",
                                FloorLevel = 0
                            }
                        }
                    }
                }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);
        Location payload = await response.Content.ReadFromJsonAsync<Location>();
        Location persisted = await new LocationLogic().GetByIdAsync("maison-annexe");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(payload);
        Assert.AreEqual("maison-annexe", payload.Id);
        Assert.AreEqual(1, payload.Zones.Count);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.Zones[0].Id));
        Assert.AreEqual(payload.Zones[0].Id.ToUpperInvariant(), payload.Zones[0].Id);
        Assert.AreEqual(1, payload.Zones[0].Rooms.Count);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.Zones[0].Rooms[0].Id));
        Assert.IsNotNull(persisted);
        Assert.AreEqual("Maison annexe", persisted.Name);
        Assert.AreEqual(1, persisted.Zones.Count);
    }

    private static async Task SeedUsersAsync()
    {
        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            FirstName = "Sarah",
            Name = "Connor",
            CommonName = "Sarah",
            IsAdmin = false,
            IsMain = true,
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");
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