using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Mesh;

[TestClass]
[DoNotParallelize]
public sealed class AutomationMeshScenarioPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task ScenarioLifecycleMethods_ShouldPersistScenarioChangesOnLocalMesh()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        AutomationMeshLogic logic = new AutomationMeshLogic();

        await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");

        AutomationMeshGlobalScenario createdScenario = await logic.UpsertGlobalScenarioAsync(new AutomationMeshGlobalScenario()
        {
            Code = "AWAY",
            Label = "Away",
            Images =
            {
                ["cover"] = "https://cdn.test/away-cover.png"
            }
        });

        AutomationMeshGlobalScenario updatedScenario = await logic.UpsertGlobalScenarioAsync(new AutomationMeshGlobalScenario()
        {
            Code = "away",
            Label = "Away updated"
        });

        bool scenarioSelected = await logic.SetCurrentGlobalScenarioAsync("AWAY");
        bool scenarioCleared = await logic.ClearCurrentGlobalScenarioAsync();
        bool scenarioDeleted = await logic.DeleteGlobalScenarioAsync("away");

        AutomationMesh storedMesh = await logic.GetLocalAsync();

        Assert.IsNotNull(createdScenario);
        Assert.IsNotNull(updatedScenario);
        Assert.AreEqual("away", createdScenario.Code);
        Assert.AreEqual("away", updatedScenario.Code);
        Assert.AreEqual("Away updated", updatedScenario.Label);
        Assert.AreEqual("https://cdn.test/away-cover.png", updatedScenario.Images["cover"]);
        Assert.IsTrue(scenarioSelected);
        Assert.IsTrue(scenarioCleared);
        Assert.IsTrue(scenarioDeleted);
        Assert.IsNotNull(storedMesh);
        Assert.IsNull(storedMesh.CurrentScenario);
        Assert.AreEqual(0, storedMesh.Scenarios.Count);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PrivacyModeMethods_ShouldPersistPrivacyModeChangesOnLocalMesh()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        AutomationMeshLogic logic = new AutomationMeshLogic();

        await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");

        bool enabled = await logic.SetPrivacyModeAsync(AutomationMeshPrivacyMode.Medium);
        bool enabledAgain = await logic.SetPrivacyModeAsync(AutomationMeshPrivacyMode.Medium);
        bool isEnabled = await logic.IsPrivacyModeEnabledAsync();
        bool cleared = await logic.ClearPrivacyModeAsync();
        bool isEnabledAfterClear = await logic.IsPrivacyModeEnabledAsync();

        AutomationMesh storedMesh = await logic.GetLocalAsync();

        Assert.IsTrue(enabled);
        Assert.IsFalse(enabledAgain);
        Assert.IsTrue(isEnabled);
        Assert.IsTrue(cleared);
        Assert.IsFalse(isEnabledAfterClear);
        Assert.IsNotNull(storedMesh);
        Assert.IsNull(storedMesh.CurrentPrivacyMode);
    }
}