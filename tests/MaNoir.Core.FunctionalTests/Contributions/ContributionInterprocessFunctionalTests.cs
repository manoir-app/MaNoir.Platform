using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.Contributions;
using MaNoir.Core.FunctionalTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NATS.Client;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Contributions;

[TestClass]
[DoNotParallelize]
public sealed class ContributionInterprocessFunctionalTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task ConfigureContributionInstanceAsync_ShouldRoundTripThroughNatsAndPersistReturnedInstance()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        await using NatsFunctionalTestHost natsHost = new NatsFunctionalTestHost();
        await natsHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope hostScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_HOST", natsHost.Host);
        using ProcessEnvironmentVariableScope portScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_PORT", natsHost.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new ProcessEnvironmentVariableScope("NATS_PORT_4222_TCP_PROTO", null);

        ContributionLogic logic = new ContributionLogic();
        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true, CanInstallMultipleTimes = false }
        ]);

        ContributionInstance instance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "sarah.hue",
            Label = "Hue"
        });

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(natsHost.ConnectionString);
        using IAsyncSubscription subscription = connection.SubscribeAsync("sarah.contribution.configure", (sender, args) =>
        {
            ContributionConfigurationMessage request = BaseMessage.ReadAs<ContributionConfigurationMessage>(Encoding.UTF8.GetString(args.Message.Data));
            ContributionConfigurationResponse response = new ContributionConfigurationResponse(request)
            {
                Response = "ok",
                IsFinalStep = true,
                Instance = request.Instance
            };
            response.Instance.IsConfigured = true;
            response.Instance.Status = ContributionInstanceStatus.Functional;
            response.Instance.StatusMessage = "Bridge reachable.";
            response.Instance.Settings["bridgeIp"] = request.SetupValues["bridgeIp"];

            args.Message.Respond(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response)));
        });
        connection.Flush();

        ContributionConfigurationResponse configured = await logic.ConfigureContributionInstanceAsync(instance.Id, new Dictionary<string, string>()
        {
            ["bridgeIp"] = "192.168.1.20"
        });
        ContributionInstance reloaded = await logic.GetContributionInstanceAsync(instance.Id);

        Assert.IsNotNull(configured);
        Assert.IsNotNull(reloaded);
        Assert.IsTrue(configured.IsFinalStep);
        Assert.IsTrue(reloaded.IsConfigured);
        Assert.AreEqual(ContributionInstanceStatus.Functional, reloaded.Status);
        Assert.AreEqual("Bridge reachable.", reloaded.StatusMessage);
        Assert.AreEqual("192.168.1.20", reloaded.Settings["bridgeIp"]);
    }
}