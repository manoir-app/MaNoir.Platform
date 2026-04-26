using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using MaNoir.Core.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Mesh;

[TestClass]
[DoNotParallelize]
public sealed class AutomationMeshPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetOrCreateLocalMesh_ShouldCreateAndRepairLocalMeshInMongo()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        AutomationMeshLogic bll = new AutomationMeshLogic();
        MongoDbHelper mongo = new MongoDbHelper();

        AutomationMesh mesh = await bll.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");
        AutomationMeshLogic.AssociateAccount(mesh, Guid.Parse("11111111-1111-1111-1111-111111111111"), "Main account", "demo");
        AutomationMeshLogic.ApplySourceCodeIntegration(mesh, new AutomationMeshSouceCodeIntegration()
        {
            GitRepoKind = "github",
            GitRepoUrl = "https://github.com/manoir-app/home-automation",
            GitBranch = "main"
        });

        AutomationMeshLogic.UpsertInternetConnection(mesh, new InternetConnectionStatusRefresh()
        {
            ConnectionId = "wan-1",
            ConnectionType = "fiber",
            Status = ConnectionStatus.Up,
            Message = "Healthy",
            Ssids = ["Manoir-Wifi"]
        }, new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero));

        AutomationMeshLogic.RefreshInternetConnectionStatus(mesh);

        await bll.SaveAsync(mesh);

        mesh.PublicId = null;
        await bll.SaveAsync(mesh);

        AutomationMesh repairedMesh = await bll.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");

        AutomationMesh storedMesh = await bll.GetLocalAsync();
        string collectionName = mongo.GetCollection<AutomationMesh>().CollectionNamespace.CollectionName;
        BsonDocument storedDocument = await mongo.GetCollection(collectionName).Find(new BsonDocument("_id", "local")).FirstOrDefaultAsync();

        Assert.IsNotNull(repairedMesh);
        Assert.IsNotNull(storedMesh);
        Assert.IsNotNull(storedDocument);
        Assert.IsFalse(string.IsNullOrWhiteSpace(repairedMesh.PublicId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(storedMesh.PublicId));
        Assert.AreEqual("https://home.demo.manoir.app/", storedMesh.MainServer.MainRole.Uri);
        Assert.AreEqual("github", storedMesh.SourceCodeIntegration.GitRepoKind);
        Assert.AreEqual("Manoir-Wifi", storedMesh.MainSsid);
        Assert.AreEqual(AutomationMeshStatus.StatusOK, storedMesh.Status.InternetConnectionStatusCode);
        Assert.AreEqual(1, storedMesh.InternetConnections.Count);
        Assert.AreEqual("wan-1", storedMesh.InternetConnections[0].Id);
        Assert.AreEqual(BsonBinarySubType.UuidLegacy, storedDocument["ManoirAppAccount"]["AccountGuid"].AsBsonBinaryData.SubType);
        Assert.IsTrue(storedDocument["MainServer"].AsBsonDocument.Contains("_id"), storedDocument.ToJson());
        Assert.IsTrue(storedDocument["InternetConnections"][0]["LastUpdate"].IsBsonArray, storedDocument.ToJson());
    }
}