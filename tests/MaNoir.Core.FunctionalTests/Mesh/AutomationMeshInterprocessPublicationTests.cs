using System.Net;
using System.Text;
using System.Threading.Tasks;
using Home.Common.Messages;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NATS.Client;
using Newtonsoft.Json;

namespace MaNoir.Core.FunctionalTests.Mesh;

[TestClass]
public sealed class AutomationMeshInterprocessPublicationTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task UpdatePublicBaseDomainAsync_ShouldPublishChangedMessageToNats()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        await using NatsFunctionalTestHost natsHost = new NatsFunctionalTestHost();
        await natsHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope hostScope = new("NATS_SERVICE_HOST", natsHost.Host);
        using ProcessEnvironmentVariableScope portScope = new("NATS_SERVICE_PORT", natsHost.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new("NATS_PORT_4222_TCP_PROTO", null);

        AutomationMeshLogic logic = new AutomationMeshLogic();
        await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(natsHost.ConnectionString);
        using ISyncSubscription subscription = connection.SubscribeSync(MeshPublicBaseDomainChangedMessage.TopicName);

        bool changed = await logic.UpdatePublicBaseDomainAsync("local", "ChezMoi.Mondomaine.FR.");
        Msg message = subscription.NextMessage(5000);
        MeshPublicBaseDomainChangedMessage payload = JsonConvert.DeserializeObject<MeshPublicBaseDomainChangedMessage>(Encoding.UTF8.GetString(message.Data));

        Assert.IsTrue(changed);
        Assert.IsNotNull(message);
        Assert.IsNotNull(payload);
        Assert.AreEqual("local", payload.MeshId);
        Assert.IsNull(payload.PreviousPublicBaseDomain);
        Assert.AreEqual("chezmoi.mondomaine.fr", payload.PublicBaseDomain);
    }
}