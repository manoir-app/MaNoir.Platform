using MaNoir.Core.AdminNavigation;
using MaNoir.Core.Api;
using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.AdminUi;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Contributions;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class AdminNavigationApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetDomainsAndDomainDetail_ShouldExposeAccessibleDomainsAndGroupedSidebarPages()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await SeedUsersAsync();
        await SeedAdminAsync();

        await new AuthorizationLogic().ReplaceUserAuthorizationAsync("sarah",
        [
            new UserZoneAccess() { ZoneId = CoreAccessZones.CoreAdminUi, Level = AccessLevel.Read }
        ]);

        await PublishCatalogsAsync();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string token = await LoginAsync(client, "sarah");

        using HttpRequestMessage domainsRequest = new(HttpMethod.Get, "/api/core/system/admin-navigation");
        domainsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage domainsResponse = await client.SendAsync(domainsRequest);
        AdminNavigationDomainsResponse domains = await domainsResponse.Content.ReadFromJsonAsync<AdminNavigationDomainsResponse>();

        Assert.AreEqual(HttpStatusCode.OK, domainsResponse.StatusCode);
        Assert.IsNotNull(domains);
        Assert.AreEqual(1, domains.Domains.Count);
        Assert.AreEqual("platform", domains.Domains[0].Id);
        Assert.AreEqual("Platform", domains.Domains[0].Label);
        Assert.AreEqual("platform", domains.Domains[0].Icon);
        Assert.AreEqual("/platform/mesh/status", domains.Domains[0].Href);

        using HttpRequestMessage domainRequest = new(HttpMethod.Get, "/api/core/system/admin-navigation/domains/platform");
        domainRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage domainResponse = await client.SendAsync(domainRequest);
        AdminDomainNavigationResponse domain = await domainResponse.Content.ReadFromJsonAsync<AdminDomainNavigationResponse>();

        Assert.AreEqual(HttpStatusCode.OK, domainResponse.StatusCode);
        Assert.IsNotNull(domain);
        Assert.AreEqual("platform", domain.Domain.Id);
        CollectionAssert.AreEqual(new[] { "Mesh", "Surveillance", "Extensions" }, domain.Sections.Select(section => section.Label).ToList());
        CollectionAssert.AreEqual(new[] { "Statut general", "Lieux et pieces" }, domain.Sections[0].Pages.Select(page => page.Label).ToList());
        Assert.AreEqual("/platform/surveillance/agents", domain.Sections[1].Pages[0].Href);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetDomains_ShouldAutoPromoteSingleMainUserWhenLegacyDatabaseHasNoAdmin()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await SeedUsersAsync();
        await PublishCatalogsAsync();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string token = await LoginAsync(client, "sarah");

        using HttpRequestMessage domainsRequest = new(HttpMethod.Get, "/api/core/system/admin-navigation");
        domainsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage domainsResponse = await client.SendAsync(domainsRequest);
        AdminNavigationDomainsResponse domains = await domainsResponse.Content.ReadFromJsonAsync<AdminNavigationDomainsResponse>();
        User promotedUser = await new UserLogic().GetByIdAsync("sarah");

        Assert.AreEqual(HttpStatusCode.OK, domainsResponse.StatusCode);
        Assert.IsNotNull(domains);
        CollectionAssert.AreEquivalent(new[] { "platform", "home-automation" }, domains.Domains.Select(domain => domain.Id).ToList());
        Assert.IsNotNull(promotedUser);
        Assert.IsTrue(promotedUser.IsAdmin);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetDomain_ShouldPreferRelativePathWhenAdminPageDoesNotDeclareLegacyUrl()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await SeedUsersAsync();
        await SeedAdminAsync();
        await PublishCatalogsAsync();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string token = await LoginAsync(client, "root");

        using HttpRequestMessage domainRequest = new(HttpMethod.Get, "/api/core/system/admin-navigation/domains/home-automation");
        domainRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage domainResponse = await client.SendAsync(domainRequest);
        AdminDomainNavigationResponse domain = await domainResponse.Content.ReadFromJsonAsync<AdminDomainNavigationResponse>();

        Assert.AreEqual(HttpStatusCode.OK, domainResponse.StatusCode);
        Assert.IsNotNull(domain);
        Assert.AreEqual("home-automation", domain.Domain.Id);
        Assert.AreEqual("/admin", domain.Domain.Href);
        Assert.AreEqual("/admin", domain.Sections[0].Pages[0].Href);
    }

    private static async Task PublishCatalogsAsync()
    {
        ContributionLogic contributionLogic = new ContributionLogic();

        await contributionLogic.PublishPluginCatalogAsync(new InstalledPlugin()
        {
            Id = "core",
            Label = "Core",
            Version = "1.0.0"
        }, CorePluginDescriptorProvider.Create("1.0.0").Contributions);

        await contributionLogic.PublishPluginCatalogAsync(new InstalledPlugin()
        {
            Id = "home-automation",
            Label = "Home Automation",
            Version = "1.0.0"
        },
        [
            new ContributionDefinition()
            {
                Id = "home-automation.admin.pages",
                Kind = ContributionKind.AdminUiPage,
                Label = "Home automation admin",
                AdminUi = new AdminUiContributionDefinitionData()
                {
                    Domain = "Home Automation",
                    AccessZoneId = "home-automation.admin-ui",
                    RequiredAccessLevel = AccessLevel.Read,
                    Pages =
                    [
                        new AdminUiPageDefinition()
                        {
                            Category = "Overview",
                            Name = "Dashboard",
                            RelativePath = "/admin",
                            Labels =
                            {
                                ["fr-FR"] = "Tableau de bord"
                            }
                        }
                    ]
                }
            }
        ]);
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

    private static async Task SeedAdminAsync()
    {
        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "root",
            FirstName = "Root",
            Name = "Admin",
            CommonName = "Root",
            IsAdmin = true,
            IsMain = true,
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("root", "P@ssword-42");
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