using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MaNoir.Core.UnitTests.Mesh;

[TestClass]
public sealed class AutomationMeshLogicScenarioTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void UpsertGlobalScenario_ShouldNormalizeCodeAndPreserveExistingImages()
    {
        AutomationMesh mesh = new AutomationMesh();
        mesh.Scenarios.Add(new AutomationMeshGlobalScenario()
        {
            Code = "away",
            Label = "Away",
            Images =
            {
                ["cover"] = "https://cdn.test/away-cover.png"
            }
        });

        AutomationMeshGlobalScenario updatedScenario = AutomationMeshLogic.UpsertGlobalScenario(mesh, new AutomationMeshGlobalScenario()
        {
            Code = "AWAY",
            Label = "Away updated"
        });

        Assert.IsNotNull(updatedScenario);
        Assert.AreEqual("away", updatedScenario.Code);
        Assert.AreEqual("Away updated", updatedScenario.Label);
        Assert.AreEqual(1, mesh.Scenarios.Count);
        Assert.AreEqual("https://cdn.test/away-cover.png", mesh.Scenarios[0].Images["cover"]);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SetCurrentGlobalScenario_ShouldUseCanonicalStoredCode()
    {
        AutomationMesh mesh = new AutomationMesh();
        mesh.Scenarios.Add(new AutomationMeshGlobalScenario()
        {
            Code = "night",
            Label = "Night"
        });

        bool changed = AutomationMeshLogic.SetCurrentGlobalScenario(mesh, "NIGHT");

        Assert.IsTrue(changed);
        Assert.AreEqual("night", mesh.CurrentScenario);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeleteGlobalScenario_ShouldRemoveMatchingScenarioIgnoringCase()
    {
        AutomationMesh mesh = new AutomationMesh();
        mesh.Scenarios.Add(new AutomationMeshGlobalScenario()
        {
            Code = "day",
            Label = "Day"
        });

        bool deleted = AutomationMeshLogic.DeleteGlobalScenario(mesh, "DAY");

        Assert.IsTrue(deleted);
        Assert.AreEqual(0, mesh.Scenarios.Count);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SetPrivacyMode_ShouldToggleEnabledState()
    {
        AutomationMesh mesh = new AutomationMesh();

        bool changed = AutomationMeshLogic.SetPrivacyMode(mesh, AutomationMeshPrivacyMode.Medium);

        Assert.IsTrue(changed);
        Assert.IsTrue(AutomationMeshLogic.IsPrivacyModeEnabled(mesh));
        Assert.AreEqual(AutomationMeshPrivacyMode.Medium, mesh.CurrentPrivacyMode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ClearPrivacyMode_ShouldResetCurrentPrivacyMode()
    {
        AutomationMesh mesh = new AutomationMesh()
        {
            CurrentPrivacyMode = AutomationMeshPrivacyMode.High
        };

        bool changed = AutomationMeshLogic.ClearPrivacyMode(mesh);

        Assert.IsTrue(changed);
        Assert.IsFalse(AutomationMeshLogic.IsPrivacyModeEnabled(mesh));
        Assert.IsNull(mesh.CurrentPrivacyMode);
    }
}