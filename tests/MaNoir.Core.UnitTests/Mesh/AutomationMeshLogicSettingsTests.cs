using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MaNoir.Core.UnitTests.Mesh;

[TestClass]
public sealed class AutomationMeshLogicSettingsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void ApplySettings_ShouldUpdateLanguageAndTimeZoneWhenValuesChange()
    {
        AutomationMesh mesh = new AutomationMesh()
        {
            LanguageId = "fr-FR",
            TimeZoneId = "Europe/Paris"
        };

        bool changed = AutomationMeshLogic.ApplySettings(mesh, "en-US", "America/New_York");

        Assert.IsTrue(changed);
        Assert.AreEqual("en-US", mesh.LanguageId);
        Assert.AreEqual("America/New_York", mesh.TimeZoneId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ApplySettings_ShouldReturnFalseWhenValuesAreUnchanged()
    {
        AutomationMesh mesh = new AutomationMesh()
        {
            LanguageId = "fr-FR",
            TimeZoneId = "Europe/Paris"
        };

        bool changed = AutomationMeshLogic.ApplySettings(mesh, "fr-FR", "Europe/Paris");

        Assert.IsFalse(changed);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void NormalizeLanguageId_ShouldReturnCanonicalCultureName()
    {
        string normalizedLanguageId = AutomationMeshLogic.NormalizeLanguageId("fr-fr");

        Assert.AreEqual("fr-FR", normalizedLanguageId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void NormalizeLanguageId_ShouldReturnNullWhenCultureIsUnknown()
    {
        string normalizedLanguageId = AutomationMeshLogic.NormalizeLanguageId("zz-ZZ");

        Assert.IsNull(normalizedLanguageId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void NormalizeIanaTimeZoneId_ShouldAcceptValidIanaIdentifier()
    {
        string normalizedTimeZoneId = AutomationMeshLogic.NormalizeIanaTimeZoneId("Europe/Paris");

        Assert.AreEqual("Europe/Paris", normalizedTimeZoneId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void NormalizeIanaTimeZoneId_ShouldRejectWindowsTimeZoneIdentifier()
    {
        string normalizedTimeZoneId = AutomationMeshLogic.NormalizeIanaTimeZoneId("Romance Standard Time");

        Assert.IsNull(normalizedTimeZoneId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SetLocationId_ShouldUpdateLocationWhenIdentifierChanges()
    {
        AutomationMesh mesh = new AutomationMesh()
        {
            LocationId = "home"
        };

        bool changed = AutomationMeshLogic.SetLocationId(mesh, "OFFICE");

        Assert.IsTrue(changed);
        Assert.AreEqual("office", mesh.LocationId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SetLocationId_ShouldReturnFalseWhenIdentifierIsMissing()
    {
        AutomationMesh mesh = new AutomationMesh();

        bool changed = AutomationMeshLogic.SetLocationId(mesh, null);

        Assert.IsFalse(changed);
        Assert.IsNull(mesh.LocationId);
    }
}