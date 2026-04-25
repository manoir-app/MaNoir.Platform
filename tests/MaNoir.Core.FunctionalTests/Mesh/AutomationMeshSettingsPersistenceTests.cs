using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.Locations;
using MaNoir.Core.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Mesh;

[TestClass]
[DoNotParallelize]
public sealed class AutomationMeshSettingsPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task SettingsMethods_ShouldPersistLanguageTimeZoneAndLocationChanges()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        AutomationMeshLogic logic = new AutomationMeshLogic();
        LocationLogic locationLogic = new LocationLogic();

        await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");
        await locationLogic.UpsertAsync(new Location()
        {
            Id = "PARIS-HOME",
            Name = "Paris home"
        });

        bool invalidLanguageChanged = await logic.UpdateSettingsAsync("LOCAL", "zz-ZZ", "Europe/Paris");
        bool invalidTimeZoneChanged = await logic.UpdateSettingsAsync("LOCAL", "fr-FR", "Romance Standard Time");
        bool settingsChanged = await logic.UpdateSettingsAsync("LOCAL", "fr-fr", "Europe/Paris");
        bool settingsChangedAgain = await logic.UpdateSettingsAsync("local", "fr-FR", "Europe/Paris");
        bool missingLocationChanged = await logic.SetLocationAsync("local", "missing-location");
        bool locationChanged = await logic.SetLocationAsync("local", "PARIS-HOME");
        bool locationChangedAgain = await logic.SetLocationAsync("local", "paris-home");

        AutomationMesh storedMesh = await logic.GetLocalAsync();

        Assert.IsFalse(invalidLanguageChanged);
        Assert.IsFalse(invalidTimeZoneChanged);
        Assert.IsTrue(settingsChanged);
        Assert.IsFalse(settingsChangedAgain);
        Assert.IsFalse(missingLocationChanged);
        Assert.IsTrue(locationChanged);
        Assert.IsFalse(locationChangedAgain);
        Assert.IsNotNull(storedMesh);
        Assert.AreEqual("fr-FR", storedMesh.LanguageId);
        Assert.AreEqual("Europe/Paris", storedMesh.TimeZoneId);
        Assert.AreEqual("paris-home", storedMesh.LocationId);
    }
}