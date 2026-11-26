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
