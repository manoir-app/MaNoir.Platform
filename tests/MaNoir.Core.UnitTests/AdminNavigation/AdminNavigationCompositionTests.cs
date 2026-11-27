using MaNoir.Core.AdminNavigation;
using MaNoir.Core.Contracts.Models.AdminUi;
using MaNoir.Core.Contracts.Models.Contributions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaNoir.Core.UnitTests.AdminNavigation;

[TestClass]
public sealed class AdminNavigationCompositionTests
{
    [TestMethod]
    public async Task ComposeNavigation_ShouldShowContributionPagesInEachOfItsDomains()
    {
        List<AdminDomainNavigationResponse> navigation = await ComposeAsync(CreatePlugin("meals", ["kitchen", "today"]));

        CollectionAssert.AreEqual(new[] { "today", "kitchen" }, navigation.Select(domain => domain.Domain.Id).ToList());
        foreach (AdminDomainNavigationResponse domain in navigation)
        {
            Assert.AreEqual("/meals", domain.Domain.Href);
            Assert.AreEqual("meals.admin.pages", domain.Sections.Single().Pages.Single().ContributionId);
        }
    }

    [TestMethod]
    public async Task ComposeNavigation_ShouldListPagesOnceWhenDomainsResolveToTheSameOne()
    {
        List<AdminDomainNavigationResponse> navigation = await ComposeAsync(CreatePlugin("meals", ["home", "inventory", "Home Automation"]));

        Assert.AreEqual("home", navigation.Single().Domain.Id);
        Assert.AreEqual(1, navigation.Single().Sections.Single().Pages.Count);
    }

    [TestMethod]
    public async Task ComposeNavigation_ShouldHideContributionWhenRequiredPluginIsMissing()
    {
        List<AdminDomainNavigationResponse> navigation = await ComposeAsync(CreatePlugin("meals", ["kitchen"], "inventory"));

        Assert.AreEqual(0, navigation.Count);
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, false)]
    public async Task ComposeNavigation_ShouldHideContributionWhenRequiredPluginIsDisabledOrUnhealthy(bool isEnabled, bool isHealthy)
    {
        List<AdminDomainNavigationResponse> navigation = await ComposeAsync(
            CreatePlugin("meals", ["kitchen"], "inventory"),
            new InstalledPlugin() { Id = "inventory", IsEnabled = isEnabled, IsHealthy = isHealthy });

        Assert.AreEqual(0, navigation.Count);
    }

    [TestMethod]
    public async Task ComposeNavigation_ShouldShowContributionWhenEveryRequiredPluginIsAvailable()
    {
        List<AdminDomainNavigationResponse> navigation = await ComposeAsync(
            CreatePlugin("meals", ["kitchen"], "Inventory", "calendar"),
            new InstalledPlugin() { Id = "inventory", IsHealthy = true },
            new InstalledPlugin() { Id = "calendar", IsHealthy = true });

        Assert.AreEqual("kitchen", navigation.Single().Domain.Id);
    }

    [TestMethod]
    public async Task ComposeNavigation_ShouldHideContributionWhenOneOfSeveralRequiredPluginsIsMissing()
    {
        List<AdminDomainNavigationResponse> navigation = await ComposeAsync(
            CreatePlugin("meals", ["kitchen"], "inventory", "calendar"),
            new InstalledPlugin() { Id = "inventory", IsHealthy = true });

        Assert.AreEqual(0, navigation.Count);
    }

    [TestMethod]
    [DataRow("/home-automation", "/overview", "/home-automation/overview")]
    [DataRow("home-automation/", "overview", "/home-automation/overview")]
    [DataRow("/inventory", "/inventory/overview", "/inventory/overview")]
    [DataRow("/inventory", "/inventory", "/inventory")]
    [DataRow("/home", "/homework", "/home/homework")]
    public async Task ComposeNavigation_ShouldPlaceModulePagesUnderTheirPublicBasePath(string basePath, string relativePath, string expectedHref)
    {
        InstalledPlugin plugin = CreatePlugin("meals", ["kitchen"]);
        plugin.AdminUiPathPrefix = basePath;
        plugin.Contributions[0].AdminUi.Pages[0].RelativePath = relativePath;

        AdminDomainNavigationResponse domain = (await ComposeAsync(plugin)).Single();

        Assert.AreEqual(expectedHref, domain.Sections.Single().Pages.Single().Href);
        Assert.IsTrue(domain.Sections.Single().Pages.Single().IsHostedByModule);
        Assert.AreEqual(expectedHref, domain.Domain.Href);
        Assert.IsTrue(domain.Domain.IsHostedByModule);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("/")]
    public async Task ComposeNavigation_ShouldKeepPagesOfPluginsServedFromTheRoot(string basePath)
    {
        InstalledPlugin plugin = CreatePlugin("meals", ["kitchen"]);
        plugin.AdminUiPathPrefix = basePath;

        AdminNavigationPage page = (await ComposeAsync(plugin)).Single().Sections.Single().Pages.Single();

        Assert.AreEqual("/meals", page.Href);
        Assert.IsFalse(page.IsHostedByModule);
    }

    [TestMethod]
    public async Task ComposeNavigation_ShouldNotPrefixPagesDeclaredWithAUrl()
    {
        InstalledPlugin plugin = CreatePlugin("core", ["settings"]);
        plugin.AdminUiPathPrefix = "/platform";
        plugin.Contributions[0].AdminUi.Pages = [new AdminUiPageDefinition() { Category = "Overview", Name = "Status", Url = "/status" }];

        AdminNavigationPage page = (await ComposeAsync(plugin)).Single().Sections.Single().Pages.Single();

        Assert.AreEqual("/status", page.Href);
        Assert.IsFalse(page.IsHostedByModule);
    }

    [TestMethod]
    public async Task ComposeNavigation_ShouldOpenADomainOnACorePageWhenItHasOne()
    {
        InstalledPlugin module = CreatePlugin("meals", ["settings"]);
        module.AdminUiPathPrefix = "/meals-app";
        InstalledPlugin core = CreatePlugin("core", ["settings"]);

        AdminDomainNavigationResponse domain = (await ComposeAsync(module, core)).Single();

        Assert.AreEqual("/core", domain.Domain.Href);
        Assert.IsFalse(domain.Domain.IsHostedByModule);
    }

    private static Task<List<AdminDomainNavigationResponse>> ComposeAsync(params InstalledPlugin[] plugins)
    {
        return AdminNavigationLogic.ComposeNavigationAsync(plugins, _ => Task.FromResult(true));
    }

    private static InstalledPlugin CreatePlugin(string pluginId, List<string> domains, params string[] requires)
    {
        return new InstalledPlugin()
        {
            Id = pluginId,
            IsHealthy = true,
            Contributions =
            [
                new ContributionDefinition()
                {
                    Id = pluginId + ".admin.pages",
                    PluginId = pluginId,
                    Kind = ContributionKind.AdminUiPage,
                    Requires = [.. requires],
                    AdminUi = new AdminUiContributionDefinitionData()
                    {
                        Domains = domains,
                        Pages = [new AdminUiPageDefinition() { Category = "Overview", Name = "Menu", RelativePath = "/" + pluginId }]
                    }
                }
            ]
        };
    }
}
