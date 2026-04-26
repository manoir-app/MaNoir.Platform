using MaNoir.Core.Api;
using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.Contributions;
using MaNoir.Core.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Authorization;

[TestClass]
public sealed class AuthorizationCatalogPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task PublishAccessZoneDefinitionsAsync_ShouldDeduplicateAndReplaceThePluginRightsCatalog()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        AuthorizationLogic logic = new AuthorizationLogic();

        List<AccessZoneDefinition> firstPublication = await logic.PublishAccessZoneDefinitionsAsync("core",
        [
            new AccessZoneDefinition() { Id = "core.authorization", Label = "Authorization" },
            new AccessZoneDefinition() { Id = "CORE.AUTHORIZATION", Label = "Authorization duplicate" },
            new AccessZoneDefinition() { Id = "core.admin-ui", Label = "Admin UI" }
        ]);

        Assert.AreEqual(2, firstPublication.Count);
        Assert.AreEqual("core.admin-ui", firstPublication[0].Id);
        Assert.AreEqual("core.authorization", firstPublication[1].Id);
        Assert.AreEqual("Authorization duplicate", firstPublication.Single(definition => definition.Id == "core.authorization").Label);

        List<AccessZoneDefinition> secondPublication = await logic.PublishAccessZoneDefinitionsAsync("core",
        [
            new AccessZoneDefinition() { Id = "core.admin-ui", Label = "Admin UI updated" }
        ]);

        Assert.AreEqual(1, secondPublication.Count);
        Assert.AreEqual("core.admin-ui", secondPublication[0].Id);
        Assert.AreEqual("Admin UI updated", secondPublication[0].Label);

        List<AccessZoneDefinition> allDefinitions = await logic.GetAccessZoneDefinitionsAsync("core");
        Assert.AreEqual(1, allDefinitions.Count);
        Assert.AreEqual("core.admin-ui", allDefinitions[0].Id);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task UseMaNoirCoreApi_ShouldRegisterCoreAccessZoneDefinitionsOnStartup()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        List<AccessZoneDefinition> definitions = await new AuthorizationLogic().GetAccessZoneDefinitionsAsync("core");
        InstalledPlugin plugin = await new MaNoir.Core.Contributions.ContributionLogic().GetInstalledPluginAsync("core");

        Assert.AreEqual(2, definitions.Count);
        Assert.AreEqual(CoreAccessZones.CoreAdminUi, definitions[0].Id);
        Assert.AreEqual(CoreAccessZones.CoreAuthorization, definitions[1].Id);
        Assert.IsNotNull(plugin);
        Assert.AreEqual(1, plugin.Contributions.Count);
        Assert.AreEqual("core.admin.pages", plugin.Contributions[0].Id);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task RegisterPlugin_ShouldLetAnotherPluginPublishItsRightsAtStartup()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await using WebApplication app = CreateApplication(registerRightsOnlyPlugin: true);
        await app.StartAsync();

        List<AccessZoneDefinition> coreDefinitions = await new AuthorizationLogic().GetAccessZoneDefinitionsAsync("core");
        List<AccessZoneDefinition> weatherDefinitions = await new AuthorizationLogic().GetAccessZoneDefinitionsAsync("weather");

        Assert.AreEqual(2, coreDefinitions.Count);
        Assert.AreEqual(1, weatherDefinitions.Count);
        Assert.AreEqual("weather.forecast", weatherDefinitions[0].Id);
        Assert.AreEqual("weather", weatherDefinitions[0].PluginId);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task RegisterPlugin_ShouldPublishPluginCatalogAndRightsAtStartup()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await using WebApplication app = CreateApplication(registerDetailedPlugin: true);
        await app.StartAsync();

        InstalledPlugin plugin = await new MaNoir.Core.Contributions.ContributionLogic().GetInstalledPluginAsync("weather");
        List<AccessZoneDefinition> weatherDefinitions = await new AuthorizationLogic().GetAccessZoneDefinitionsAsync("weather");

        Assert.IsNotNull(plugin);
        Assert.AreEqual("Weather", plugin.Label);
        Assert.AreEqual(1, plugin.Contributions.Count);
        Assert.AreEqual("weather.admin.pages", plugin.Contributions[0].Id);
        Assert.AreEqual(1, weatherDefinitions.Count);
        Assert.AreEqual("weather.forecast", weatherDefinitions[0].Id);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task RegisterPlugin_ShouldAllowReferencingParentPluginRightsThroughDeclaredRepositoryDependency()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await using WebApplication app = CreateApplication(registerDependentPlugin: true);
        await app.StartAsync();

        InstalledPlugin childPlugin = await new MaNoir.Core.Contributions.ContributionLogic().GetInstalledPluginAsync("weather-child");

        Assert.IsNotNull(childPlugin);
        Assert.AreEqual("https://example.net/plugins/weather-child", childPlugin.RepositoryUrl);
        Assert.AreEqual(1, childPlugin.DependencyRepositoryUrls.Count);
        Assert.AreEqual("https://example.net/plugins/weather-parent", childPlugin.DependencyRepositoryUrls[0]);
        Assert.AreEqual("weather.parent-zone", childPlugin.Contributions[0].AdminUi.AccessZoneId);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task RegisterPlugin_ShouldAllowReferencingAncestorPluginRightsThroughTransitiveRepositoryDependencies()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        await using WebApplication app = CreateApplication(registerTransitiveDependentPlugin: true);
        await app.StartAsync();

        InstalledPlugin childPlugin = await new MaNoir.Core.Contributions.ContributionLogic().GetInstalledPluginAsync("weather-grandchild");

        Assert.IsNotNull(childPlugin);
        Assert.AreEqual("weather.ancestor-zone", childPlugin.Contributions[0].AdminUi.AccessZoneId);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task RegisterPlugin_ShouldRejectReferencingExternalRightsWithoutDeclaredRepositoryDependency()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        InvalidPluginDescriptorException exception = await Assert.ThrowsExceptionAsync<InvalidPluginDescriptorException>(() => Task.FromResult(CreateApplication(registerInvalidDependentPlugin: true)));
        Assert.IsTrue(exception.Message.Contains("weather.parent-zone"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task RegisterPlugin_ShouldRejectRepositoryDependencyCycles()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        InvalidPluginDescriptorException exception = await Assert.ThrowsExceptionAsync<InvalidPluginDescriptorException>(() => Task.FromResult(CreateApplication(registerCyclicDependentPlugin: true)));
        Assert.IsTrue(exception.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(exception.Message.Contains("weather-cycle-a"));
        Assert.IsTrue(exception.Message.Contains("weather-cycle-b"));
    }

    private static WebApplication CreateApplication(bool registerRightsOnlyPlugin = false, bool registerDetailedPlugin = false, bool registerDependentPlugin = false, bool registerInvalidDependentPlugin = false, bool registerTransitiveDependentPlugin = false, bool registerCyclicDependentPlugin = false)
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
        if (registerRightsOnlyPlugin)
        {
            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather",
                AccessZones =
                [
                    new AccessZoneDefinition()
                    {
                        Id = "weather.forecast",
                        Label = "Weather forecast",
                        Description = "Access to weather forecast management."
                    }
                ]
            });
        }

        if (registerDetailedPlugin)
        {
            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather",
                Label = "Weather",
                Version = "1.2.3",
                Description = "Weather plugin.",
                Publisher = "MaNoir",
                RepositoryUrl = "https://example.net/plugins/weather",
                AccessZones =
                [
                    new AccessZoneDefinition()
                    {
                        Id = "weather.forecast",
                        Label = "Weather forecast",
                        Description = "Access to weather forecast management."
                    }
                ],
                Contributions =
                [
                    new ContributionDefinition()
                    {
                        Id = "weather.admin.pages",
                        Kind = ContributionKind.AdminUiPage,
                        Label = "Weather admin",
                        Description = "Weather administration pages.",
                        AdminUi = new AdminUiContributionDefinitionData()
                        {
                            Domain = "Weather",
                            AccessZoneId = "weather.forecast",
                            RequiredAccessLevel = AccessLevel.Read,
                            Pages =
                            [
                                new AdminUiPageDefinition()
                                {
                                    Category = "General",
                                    Name = "Forecast",
                                    Url = "/admin/weather",
                                    Labels = new Dictionary<string, string>()
                                    {
                                        ["en"] = "Weather",
                                        ["fr-FR"] = "Meteo"
                                    }
                                }
                            ]
                        }
                    }
                ]
            });
        }

        if (registerDependentPlugin || registerInvalidDependentPlugin)
        {
            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-parent",
                Label = "Weather parent",
                RepositoryUrl = "https://example.net/plugins/weather-parent",
                AccessZones =
                [
                    new AccessZoneDefinition()
                    {
                        Id = "weather.parent-zone",
                        Label = "Weather parent zone"
                    }
                ]
            });
        }

        if (registerTransitiveDependentPlugin)
        {
            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-ancestor",
                Label = "Weather ancestor",
                RepositoryUrl = "https://example.net/plugins/weather-ancestor",
                AccessZones =
                [
                    new AccessZoneDefinition()
                    {
                        Id = "weather.ancestor-zone",
                        Label = "Weather ancestor zone"
                    }
                ]
            });

            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-parent-transitive",
                Label = "Weather parent transitive",
                RepositoryUrl = "https://example.net/plugins/weather-parent-transitive",
                DependencyRepositoryUrls = ["https://example.net/plugins/weather-ancestor"],
                AccessZones =
                [
                    new AccessZoneDefinition()
                    {
                        Id = "weather.parent-transitive-zone",
                        Label = "Weather parent transitive zone"
                    }
                ]
            });

            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-grandchild",
                Label = "Weather grandchild",
                RepositoryUrl = "https://example.net/plugins/weather-grandchild",
                DependencyRepositoryUrls = ["https://example.net/plugins/weather-parent-transitive"],
                Contributions =
                [
                    new ContributionDefinition()
                    {
                        Id = "weather-grandchild.admin.pages",
                        Kind = ContributionKind.AdminUiPage,
                        Label = "Weather grandchild admin",
                        AdminUi = new AdminUiContributionDefinitionData()
                        {
                            Domain = "Weather",
                            AccessZoneId = "weather.ancestor-zone"
                        }
                    }
                ]
            });
        }

        if (registerCyclicDependentPlugin)
        {
            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-cycle-a",
                Label = "Weather cycle a",
                RepositoryUrl = "https://example.net/plugins/weather-cycle-a",
                AccessZones =
                [
                    new AccessZoneDefinition()
                    {
                        Id = "weather.cycle-a-zone",
                        Label = "Weather cycle A zone"
                    }
                ]
            });

            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-cycle-b",
                Label = "Weather cycle b",
                RepositoryUrl = "https://example.net/plugins/weather-cycle-b",
                DependencyRepositoryUrls = ["https://example.net/plugins/weather-cycle-a"],
                AccessZones =
                [
                    new AccessZoneDefinition()
                    {
                        Id = "weather.cycle-b-zone",
                        Label = "Weather cycle B zone"
                    }
                ]
            });

            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-cycle-a-updated",
                Label = "Weather cycle a updated",
                RepositoryUrl = "https://example.net/plugins/weather-cycle-a",
                DependencyRepositoryUrls = ["https://example.net/plugins/weather-cycle-b"],
                Contributions =
                [
                    new ContributionDefinition()
                    {
                        Id = "weather-cycle-a-updated.admin.pages",
                        Kind = ContributionKind.AdminUiPage,
                        Label = "Weather cycle admin",
                        AdminUi = new AdminUiContributionDefinitionData()
                        {
                            Domain = "Weather",
                            AccessZoneId = "weather.cycle-b-zone"
                        }
                    }
                ]
            });
        }

        if (registerDependentPlugin)
        {
            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-child",
                Label = "Weather child",
                RepositoryUrl = "https://example.net/plugins/weather-child",
                DependencyRepositoryUrls = ["https://example.net/plugins/weather-parent/"],
                Contributions =
                [
                    new ContributionDefinition()
                    {
                        Id = "weather-child.admin.pages",
                        Kind = ContributionKind.AdminUiPage,
                        Label = "Weather child admin",
                        AdminUi = new AdminUiContributionDefinitionData()
                        {
                            Domain = "Weather",
                            AccessZoneId = "weather.parent-zone"
                        }
                    }
                ]
            });
        }

        if (registerInvalidDependentPlugin)
        {
            app.RegisterPlugin(new PluginDescriptor()
            {
                Id = "weather-invalid-child",
                Label = "Weather invalid child",
                RepositoryUrl = "https://example.net/plugins/weather-invalid-child",
                Contributions =
                [
                    new ContributionDefinition()
                    {
                        Id = "weather-invalid-child.admin.pages",
                        Kind = ContributionKind.AdminUiPage,
                        Label = "Weather invalid child admin",
                        AdminUi = new AdminUiContributionDefinitionData()
                        {
                            Domain = "Weather",
                            AccessZoneId = "weather.parent-zone"
                        }
                    }
                ]
            });
        }

        return app;
    }
}