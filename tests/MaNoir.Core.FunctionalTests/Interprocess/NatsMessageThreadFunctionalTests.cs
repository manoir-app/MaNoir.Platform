using Home.Common;
using Home.Common.Messages;
using MaNoir.Core.FunctionalTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NATS.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Interprocess;

[TestClass]
[DoNotParallelize]
public sealed class NatsMessageThreadFunctionalTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task Push_ShouldPublishLegacyJsonToNatsBroker()
    {
        await using NatsFunctionalTestHost host = new NatsFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope hostScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_HOST", host.Host);
        using ProcessEnvironmentVariableScope portScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_PORT", host.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new ProcessEnvironmentVariableScope("NATS_PORT_4222_TCP_PROTO", null);

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(host.ConnectionString);
        using ISyncSubscription subscription = connection.SubscribeSync("tests.core.nats.push");
        connection.Flush();

        NatsMessageThread.Push(new AgentGenericMessage("tests.core.nats.push")
        {
            MessageContent = "hello"
        });

        Msg message = subscription.NextMessage(5000);
        string json = Encoding.UTF8.GetString(message.Data);
        JObject payload = JObject.Parse(json);

        Assert.AreEqual("tests.core.nats.push", message.Subject);
        Assert.AreEqual("tests.core.nats.push", payload.Value<string>("Topic"));
        Assert.AreEqual("hello", payload.Value<string>("MessageContent"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Request_ShouldRoundTripMessageResponseThroughNatsBroker()
    {
        await using NatsFunctionalTestHost host = new NatsFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope hostScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_HOST", host.Host);
        using ProcessEnvironmentVariableScope portScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_PORT", host.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new ProcessEnvironmentVariableScope("NATS_PORT_4222_TCP_PROTO", null);

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(host.ConnectionString);
        using IAsyncSubscription subscription = connection.SubscribeAsync("tests.core.nats.request", (sender, args) =>
        {
            MessageResponse response = new MessageResponse()
            {
                Topic = args.Message.Subject,
                Response = "ok"
            };

            args.Message.Respond(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response)));
        });
        connection.Flush();

        MessageResponse responsePayload = NatsMessageThread.Request<MessageResponse>(
            "tests.core.nats.request",
            new AgentGenericMessage("tests.core.nats.request") { MessageContent = "ping" },
            5000);

        Assert.IsNotNull(responsePayload);
        Assert.AreEqual("ok", responsePayload.Response);
        Assert.AreEqual("tests.core.nats.request", responsePayload.Topic);
    }
}