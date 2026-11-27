using MaNoir.Core.AdminNavigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MaNoir.Core.UnitTests.AdminNavigation;

[TestClass]
public sealed class ApplicationDomainsTests
{
    [TestMethod]
    public void All_ShouldDeclareTheFiveDomainsWithSettingsLast()
    {
        CollectionAssert.AreEqual(
            new[] { "today", "kitchen", "home", "projects", "settings" },
            ApplicationDomains.All.OrderBy(domain => domain.Order).Select(domain => domain.Id).ToList());
        Assert.AreEqual(0, ApplicationDomains.Today.Order);
        Assert.IsTrue(ApplicationDomains.Resolve("Weather").Order < ApplicationDomains.Settings.Order);
    }

    [TestMethod]
    [DataRow("today", "today")]
    [DataRow(" Kitchen ", "kitchen")]
    [DataRow("platform", "settings")]
    [DataRow("Platform", "settings")]
    [DataRow("Core", "settings")]
    [DataRow("general", "settings")]
    [DataRow("home-automation", "home")]
    [DataRow("Home Automation", "home")]
    [DataRow("HomeAutomation", "home")]
    [DataRow("Inventory", "home")]
    [DataRow("daily-life", "today")]
    [DataRow("DailyLife", "today")]
    public void Resolve_ShouldMapDeclaredIdsAndAliases(string name, string expectedId)
    {
        Assert.AreEqual(expectedId, ApplicationDomains.Resolve(name).Id);
    }

    [TestMethod]
    public void Resolve_ShouldReturnGenericDomainForUnknownName()
    {
        ApplicationDomain domain = ApplicationDomains.Resolve(" Météo locale ");

        Assert.AreEqual("meteo-locale", domain.Id);
        Assert.AreEqual("Météo locale", domain.Label);
        Assert.AreEqual("generic", domain.Icon);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("  ")]
    [DataRow("---")]
    public void Resolve_ShouldReturnNullForBlankName(string name)
    {
        Assert.IsNull(ApplicationDomains.Resolve(name));
    }

    [TestMethod]
    public void ResolveAll_ShouldRemoveAliasDuplicates()
    {
        CollectionAssert.AreEqual(
            new[] { "settings", "home" },
            ApplicationDomains.ResolveAll(["core", "platform", "inventory", "home"]).Select(domain => domain.Id).ToList());
    }

    [TestMethod]
    public void ResolveAll_ShouldFallBackToSettingsWhenNoDomainIsNamed()
    {
        Assert.AreSame(ApplicationDomains.Settings, ApplicationDomains.ResolveAll(null).Single());
        Assert.AreSame(ApplicationDomains.Settings, ApplicationDomains.ResolveAll([" "]).Single());
    }
}
