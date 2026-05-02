using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Mesh;

[TestClass]
public sealed class InternetConnectionMonitoringLogicTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task RefreshLocalConnectionAsync_ShouldPersistConnectionAndAggregateMeshStatus()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();

        using ProcessEnvironmentVariableScope connectionScope = new("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        InternetConnectionMonitoringLogic logic = new InternetConnectionMonitoringLogic();
        AutomationMeshLogic meshLogic = new AutomationMeshLogic();

        InternetConnectionMonitoringResult upResult = await logic.RefreshLocalConnectionAsync(
            new InternetConnectionStatusRefresh()
            {
                ConnectionId = "wan-main",
                ConnectionType = "internet",
                Status = ConnectionStatus.Up,
                Message = "Probe OK"
            },
            "tests-host",
            "https://localhost");

        Assert.IsNotNull(upResult);
        Assert.IsNotNull(upResult.Connection);
        Assert.AreEqual(ConnectionStatus.Up, upResult.Connection.Status);
        Assert.AreEqual(AutomationMeshStatus.StatusOK, upResult.Mesh.Status.InternetConnectionStatusCode);

        InternetConnectionMonitoringResult downResult = await logic.RefreshLocalConnectionAsync(
            new InternetConnectionStatusRefresh()
            {
                ConnectionId = "wan-main",
                ConnectionType = "internet",
                Status = ConnectionStatus.Down,
                Message = "Probe failed"
            },
            "tests-host",
            "https://localhost");

        Assert.IsNotNull(downResult);
        Assert.AreEqual(ConnectionStatus.Down, downResult.Connection.Status);
        Assert.IsTrue(downResult.MeshStatusChanged);

        AutomationMesh storedMesh = await meshLogic.GetLocalAsync();
        Assert.IsNotNull(storedMesh);
        Assert.AreEqual(AutomationMeshStatus.StatusKO, storedMesh.Status.InternetConnectionStatusCode);
        Assert.AreEqual(1, storedMesh.InternetConnections.Count);
        Assert.AreEqual("Probe failed", storedMesh.InternetConnections[0].LastMessage);
    }
}