using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.Contributions;
using MaNoir.Core.FunctionalTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Contributions;

[TestClass]
[DoNotParallelize]
public sealed class ContributionPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task PublishPluginCatalogAsync_ShouldPersistPluginAndAdminUiContributionDefinitions()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        ContributionLogic logic = new ContributionLogic();

        InstalledPlugin publishedPlugin = await logic.PublishPluginCatalogAsync(new InstalledPlugin()
        {
            Id = "Sarah",
            Label = "Sarah",
            Version = "1.0.0",
            Description = "Home automation agent",
            Publisher = "MaNoir",
            RepositoryUrl = "https://example.net/plugins/sarah/",
            DependencyRepositoryUrls = [" https://example.net/plugins/core/ "]
        },
        [
            new ContributionDefinition()
            {
                Id = "sarah.hue",
                Kind = ContributionKind.Integration,
                Label = "Philips Hue",
                Description = "Integrates Philips Hue",
                CanCreateInstances = true,
                CanInstallMultipleTimes = false,
                Tags = ["lighting", "home-automation"],
                Integration = new IntegrationContributionDefinitionData()
                {
                    Domain = "DailyLife",
                    Category = "HouseKeepingService",
                    ServiceDependencyKind = IntegrationServiceDependencyKind.Cloud,
                    RequiresExternalSubscription = true,
                    ExternalSubscriptionInfo = "Requires a Philips Hue account and bridge registration.",
                    DocumentationUrl = "https://docs.example.net/integrations/hue",
                    PublishedEntityKinds =
                    [
                        new IntegrationPublishedEntityKindDefinition()
                        {
                            Kind = "light",
                            Descriptions =
                            {
                                ["fr-FR"] = "Ampoules et luminaires pilotables.",
                                ["en-US"] = "Controllable lights and fixtures."
                            }
                        },
                        new IntegrationPublishedEntityKindDefinition()
                        {
                            Kind = "sensor:presence",
                            Descriptions =
                            {
                                ["fr-FR"] = "Capteurs de présence associés.",
                                ["en-US"] = "Associated presence sensors."
                            }
                        }
                    ]
                }
            },
            new ContributionDefinition()
            {
                Id = "sarah.admin.pages",
                Kind = ContributionKind.AdminUiPage,
                Label = "Sarah Admin Pages",
                Description = "Adds Sarah administration pages",
                CanCreateInstances = false,
                AdminUi = new AdminUiContributionDefinitionData()
                {
                    Domain = "Core",
                    AccessZoneId = "core.admin-ui.sarah",
                    RequiredAccessLevel = AccessLevel.Read,
                    Pages =
                    [
                        new AdminUiPageDefinition()
                        {
                            Category = "Lighting",
                            Name = "Overview",
                            Url = "/admin/core/sarah/overview",
                            Labels =
                            {
                                ["fr-FR"] = "Vue d'ensemble",
                                ["en-US"] = "Overview"
                            }
                        }
                    ]
                }
            }
        ]);

        InstalledPlugin reloadedPlugin = await logic.GetInstalledPluginAsync("SARAH");
        List<ContributionDefinition> definitions = await logic.GetContributionDefinitionsAsync("sarah");
        ContributionDefinition adminUiDefinition = definitions.Single(definition => definition.Kind == ContributionKind.AdminUiPage);
        ContributionDefinition integrationDefinition = definitions.Single(definition => definition.Kind == ContributionKind.Integration);

        Assert.IsNotNull(publishedPlugin);
        Assert.IsNotNull(reloadedPlugin);
        Assert.AreEqual("sarah", reloadedPlugin.Id);
        Assert.AreEqual("https://example.net/plugins/sarah", reloadedPlugin.RepositoryUrl);
        Assert.AreEqual(1, reloadedPlugin.DependencyRepositoryUrls.Count);
        Assert.AreEqual("https://example.net/plugins/core", reloadedPlugin.DependencyRepositoryUrls[0]);
        Assert.IsFalse(reloadedPlugin.HasNewFeatures);
        Assert.IsFalse(string.IsNullOrWhiteSpace(reloadedPlugin.LastPublishedCatalogFingerprint));
        Assert.AreEqual(2, reloadedPlugin.Contributions.Count);
        Assert.AreEqual(2, definitions.Count);
        Assert.AreEqual("DailyLife", integrationDefinition.Integration.Domain);
        Assert.AreEqual("HouseKeepingService", integrationDefinition.Integration.Category);
        Assert.AreEqual(IntegrationServiceDependencyKind.Cloud, integrationDefinition.Integration.ServiceDependencyKind);
        Assert.IsTrue(integrationDefinition.Integration.RequiresExternalSubscription);
        Assert.AreEqual("https://docs.example.net/integrations/hue", integrationDefinition.Integration.DocumentationUrl);
        Assert.AreEqual(2, integrationDefinition.Integration.PublishedEntityKinds.Count);
        Assert.AreEqual("Ampoules et luminaires pilotables.", integrationDefinition.Integration.PublishedEntityKinds.Single(entityKind => entityKind.Kind == "light").Descriptions["fr-FR"]);
        Assert.AreEqual("sarah", adminUiDefinition.PluginId);
        Assert.AreEqual("Core", adminUiDefinition.AdminUi.Domain);
        Assert.AreEqual("core.admin-ui.sarah", adminUiDefinition.AdminUi.AccessZoneId);
        Assert.AreEqual(AccessLevel.Read, adminUiDefinition.AdminUi.RequiredAccessLevel);
        Assert.AreEqual(1, adminUiDefinition.AdminUi.Pages.Count);
        Assert.AreEqual("/admin/core/sarah/overview", adminUiDefinition.AdminUi.Pages[0].Url);
        Assert.AreEqual("Vue d'ensemble", adminUiDefinition.AdminUi.Pages[0].Labels["fr-FR"]);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PublishPluginCatalogAsync_ShouldIgnoreOrderAndMarkChangedCatalogAsNewFeatures()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        ContributionLogic logic = new ContributionLogic();
        InstalledPlugin plugin = new InstalledPlugin() { Id = "Sarah", Label = "Sarah", Version = "1.0.0" };

        await logic.PublishPluginCatalogAsync(plugin,
        [
            new ContributionDefinition()
            {
                Id = "sarah.hue",
                Kind = ContributionKind.Integration,
                Label = "Hue",
                CanCreateInstances = true,
                Integration = new IntegrationContributionDefinitionData()
                {
                    Domain = "DailyLife",
                    Category = "HouseKeepingService",
                    ServiceDependencyKind = IntegrationServiceDependencyKind.Cloud,
                    RequiresExternalSubscription = true,
                    ExternalSubscriptionInfo = "Requires a Philips Hue account and bridge registration.",
                    DocumentationUrl = "https://docs.example.net/integrations/hue",
                    PublishedEntityKinds =
                    [
                        new IntegrationPublishedEntityKindDefinition()
                        {
                            Kind = "light",
                            Descriptions =
                            {
                                ["fr-FR"] = "Ampoules et luminaires pilotables.",
                                ["en-US"] = "Controllable lights and fixtures."
                            }
                        }
                    ]
                }
            },
            new ContributionDefinition() { Id = "sarah.admin.pages", Kind = ContributionKind.AdminUiPage, Label = "Admin", CanCreateInstances = false, AdminUi = new AdminUiContributionDefinitionData() { Domain = "Core" } }
        ]);

        InstalledPlugin sameCatalogPlugin = await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah", Version = "1.0.0" },
        [
            new ContributionDefinition() { Id = "sarah.admin.pages", Kind = ContributionKind.AdminUiPage, Label = "Admin", CanCreateInstances = false, AdminUi = new AdminUiContributionDefinitionData() { Domain = "Core" } },
            new ContributionDefinition()
            {
                Id = "sarah.hue",
                Kind = ContributionKind.Integration,
                Label = "Hue",
                CanCreateInstances = true,
                Integration = new IntegrationContributionDefinitionData()
                {
                    Domain = "DailyLife",
                    Category = "HouseKeepingService",
                    ServiceDependencyKind = IntegrationServiceDependencyKind.Cloud,
                    RequiresExternalSubscription = true,
                    ExternalSubscriptionInfo = "Requires a Philips Hue account and bridge registration.",
                    DocumentationUrl = "https://docs.example.net/integrations/hue",
                    PublishedEntityKinds =
                    [
                        new IntegrationPublishedEntityKindDefinition()
                        {
                            Kind = "light",
                            Descriptions =
                            {
                                ["fr-FR"] = "Ampoules et luminaires pilotables.",
                                ["en-US"] = "Controllable lights and fixtures."
                            }
                        }
                    ]
                }
            }
        ]);

        InstalledPlugin changedCatalogPlugin = await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah", Version = "1.0.0" },
        [
            new ContributionDefinition() { Id = "sarah.admin.pages", Kind = ContributionKind.AdminUiPage, Label = "Admin", CanCreateInstances = false, AdminUi = new AdminUiContributionDefinitionData() { Domain = "Core", Pages = [ new AdminUiPageDefinition() { Category = "General", Name = "Dashboard", Url = "/admin/core/sarah/dashboard" } ] } },
            new ContributionDefinition()
            {
                Id = "sarah.hue",
                Kind = ContributionKind.Integration,
                Label = "Hue",
                CanCreateInstances = true,
                Integration = new IntegrationContributionDefinitionData()
                {
                    Domain = "DailyLife",
                    Category = "HouseKeepingService",
                    ServiceDependencyKind = IntegrationServiceDependencyKind.Cloud,
                    RequiresExternalSubscription = true,
                    ExternalSubscriptionInfo = "Requires a Philips Hue account and bridge registration.",
                    DocumentationUrl = "https://docs.example.net/integrations/hue-v2",
                    PublishedEntityKinds =
                    [
                        new IntegrationPublishedEntityKindDefinition()
                        {
                            Kind = "light",
                            Descriptions =
                            {
                                ["fr-FR"] = "Ampoules et luminaires pilotables.",
                                ["en-US"] = "Controllable lights and fixtures."
                            }
                        }
                    ]
                }
            }
        ]);

        Assert.IsFalse(sameCatalogPlugin.HasNewFeatures);
        Assert.IsTrue(changedCatalogPlugin.HasNewFeatures);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetInstalledPluginsByContributionKindAsync_ShouldReturnOnlyMatchingPluginsWithFilteredContributions()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        ContributionLogic logic = new ContributionLogic();

        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true },
            new ContributionDefinition() { Id = "sarah.admin.pages", Kind = ContributionKind.AdminUiPage, Label = "Admin", CanCreateInstances = false, AdminUi = new AdminUiContributionDefinitionData() { Domain = "Core" } }
        ]);

        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "erza", Label = "Erza" },
        [
            new ContributionDefinition() { Id = "erza.weather", Kind = ContributionKind.Integration, Label = "Weather", CanCreateInstances = true }
        ]);

        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "aurore", Label = "Aurore" },
        [
            new ContributionDefinition() { Id = "aurore.admin.pages", Kind = ContributionKind.AdminUiPage, Label = "Admin", CanCreateInstances = false, AdminUi = new AdminUiContributionDefinitionData() { Domain = "Daily" } }
        ]);

        List<InstalledPlugin> integrationPlugins = await logic.GetInstalledPluginsByContributionKindAsync(ContributionKind.Integration);
        List<InstalledPlugin> adminUiPlugins = await logic.GetInstalledPluginsByContributionKindAsync(ContributionKind.AdminUiPage);

        Assert.AreEqual(2, integrationPlugins.Count);
        Assert.IsTrue(integrationPlugins.Any(plugin => plugin.Id == "sarah"));
        Assert.IsTrue(integrationPlugins.Any(plugin => plugin.Id == "erza"));
        Assert.IsTrue(integrationPlugins.All(plugin => plugin.Contributions.Count > 0));
        Assert.IsTrue(integrationPlugins.All(plugin => plugin.Contributions.All(contribution => contribution.Kind == ContributionKind.Integration)));
        Assert.AreEqual(1, integrationPlugins.Single(plugin => plugin.Id == "sarah").Contributions.Count);
        Assert.AreEqual("sarah.hue", integrationPlugins.Single(plugin => plugin.Id == "sarah").Contributions[0].Id);

        Assert.AreEqual(2, adminUiPlugins.Count);
        Assert.IsTrue(adminUiPlugins.All(plugin => plugin.Contributions.All(contribution => contribution.Kind == ContributionKind.AdminUiPage)));
        Assert.AreEqual("aurore.admin.pages", adminUiPlugins.Single(plugin => plugin.Id == "aurore").Contributions[0].Id);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PublishPluginCatalogAsync_ShouldArchiveInstancesWhenAContributionIsRemoved()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        ContributionLogic logic = new ContributionLogic();
        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true },
            new ContributionDefinition() { Id = "sarah.weather", Kind = ContributionKind.Integration, Label = "Weather", CanCreateInstances = true }
        ]);

        ContributionInstance instance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            Id = "instance-hue",
            ContributionDefinitionId = "sarah.hue",
            Label = "Living room Hue",
            IsConfigured = true
        });

        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.weather", Kind = ContributionKind.Integration, Label = "Weather", CanCreateInstances = true }
        ]);

        ContributionInstance archivedInstance = await logic.GetContributionInstanceAsync(instance.Id);
        ContributionDefinition removedDefinition = await logic.GetContributionDefinitionAsync("sarah.hue");

        Assert.IsNotNull(archivedInstance);
        Assert.IsFalse(archivedInstance.IsEnabled);
        Assert.AreEqual(ContributionInstanceStatus.Archived, archivedInstance.Status);
        Assert.IsTrue(archivedInstance.StatusMessage.Contains("sarah.hue"));
        Assert.IsNull(removedDefinition);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task UpsertContributionInstanceAsync_ShouldPersistInstanceForInstantiableContribution()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        ContributionLogic logic = new ContributionLogic();
        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true, CanInstallMultipleTimes = false }
        ]);

        ContributionInstance storedInstance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "SARAH.HUE",
            Label = "Living room Hue",
            IsConfigured = true,
            Settings =
            {
                ["bridgeIp"] = "192.168.1.20"
            }
        });

        ContributionInstance reloadedInstance = await logic.GetContributionInstanceAsync(storedInstance.Id);

        Assert.IsNotNull(storedInstance);
        Assert.IsNotNull(reloadedInstance);
        Assert.AreEqual("sarah", reloadedInstance.PluginId);
        Assert.AreEqual("sarah.hue", reloadedInstance.ContributionDefinitionId);
        Assert.AreEqual("Living room Hue", reloadedInstance.Label);
        Assert.AreEqual("192.168.1.20", reloadedInstance.Settings["bridgeIp"]);
        Assert.IsTrue(reloadedInstance.IsConfigured);
        Assert.AreEqual(ContributionInstanceStatus.Functional, reloadedInstance.Status);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task UpsertContributionInstanceAsync_ShouldRequireAuthorizationWhenSettingsReferenceSecrets()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        ContributionLogic logic = new ContributionLogic();
        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true, CanInstallMultipleTimes = false }
        ]);

        ContributionInstance pendingInstance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "sarah.hue",
            Label = "Authorized Hue",
            IsConfigured = true,
            Settings =
            {
                ["clientSecret"] = "{{SECRET: hue.client.secret }}"
            }
        });

        ContributionInstance authorizedInstance = await logic.AuthorizeContributionInstanceAsync(pendingInstance.Id);

        Assert.IsNotNull(pendingInstance);
        Assert.AreEqual(ContributionInstanceStatus.AuthorizationPending, pendingInstance.Status);
        Assert.IsNull(pendingInstance.AuthorizedAtUtc);
        Assert.AreEqual("Authorization required before the plugin can receive referenced secrets.", pendingInstance.StatusMessage);
        Assert.IsNotNull(authorizedInstance);
        Assert.AreEqual(ContributionInstanceStatus.Functional, authorizedInstance.Status);
        Assert.IsNotNull(authorizedInstance.AuthorizedAtUtc);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task UpsertContributionInstanceAsync_ShouldRejectNonInstantiableAndDuplicateSingleInstanceContributions()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        ContributionLogic logic = new ContributionLogic();
        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true, CanInstallMultipleTimes = false },
            new ContributionDefinition() { Id = "sarah.admin.pages", Kind = ContributionKind.AdminUiPage, Label = "Admin", CanCreateInstances = false, AdminUi = new AdminUiContributionDefinitionData() { Domain = "Core" } }
        ]);

        ContributionInstance firstInstance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "sarah.hue",
            Label = "First Hue"
        });

        ContributionInstance secondInstance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "sarah.hue",
            Label = "Second Hue"
        });

        ContributionInstance adminUiInstance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "sarah.admin.pages",
            Label = "Admin"
        });

        List<ContributionInstance> storedInstances = await logic.GetContributionInstancesAsync("sarah.hue");

        Assert.IsNotNull(firstInstance);
        Assert.IsNull(secondInstance);
        Assert.IsNull(adminUiInstance);
        Assert.AreEqual(1, storedInstances.Count);
        Assert.AreEqual("First Hue", storedInstances[0].Label);
    }
}