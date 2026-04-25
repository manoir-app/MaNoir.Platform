using Home.Graph.Common;
using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.FunctionalTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Mqtt;

[TestClass]
[DoNotParallelize]
public sealed class MqttHelperFunctionalTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task PublishMeshProperty_ShouldPublishToMosquittoBroker()
    {
        await using MosquittoFunctionalTestHost host = new MosquittoFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope hostScope = new ProcessEnvironmentVariableScope("MQTT_SERVICE_HOST", host.Host);
        using ProcessEnvironmentVariableScope portScope = new ProcessEnvironmentVariableScope("MQTT_SERVICE_PORT", host.Port.ToString());
        using ProcessEnvironmentVariableScope mosquittoHostScope = new ProcessEnvironmentVariableScope("MOSQUITTO_SERVICE_HOST", null);
        using ProcessEnvironmentVariableScope mosquittoPortScope = new ProcessEnvironmentVariableScope("MOSQUITTO_SERVICE_PORT", null);

        MqttFactory factory = new MqttFactory();
        using IMqttClient client = factory.CreateMqttClient();
        TaskCompletionSource<string> receivedPayload = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        client.ApplicationMessageReceivedAsync += args =>
        {
            receivedPayload.TrySetResult(Encoding.UTF8.GetString(args.ApplicationMessage.Payload ?? Array.Empty<byte>()));
            return Task.CompletedTask;
        };

        await client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithClientId("functional-mqtt-subscriber")
            .WithTcpServer(host.Host, host.Port)
            .Build());
        await client.SubscribeAsync("manoir/mesh/properties/privacyMode");

        try
        {
            MqttHelper.Start("functional-tests");
            MqttHelper.PublishMeshProperty("privacyMode", "none");

            string payload = await receivedPayload.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.AreEqual("none", payload);
        }
        finally
        {
            MqttHelper.Stop();
            await client.DisconnectAsync();
        }
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PublishEntity_ShouldPublishFlattenedEntityDataToMosquittoBroker()
    {
        await using MosquittoFunctionalTestHost host = new MosquittoFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope hostScope = new ProcessEnvironmentVariableScope("MQTT_SERVICE_HOST", host.Host);
        using ProcessEnvironmentVariableScope portScope = new ProcessEnvironmentVariableScope("MQTT_SERVICE_PORT", host.Port.ToString());
        using ProcessEnvironmentVariableScope mosquittoHostScope = new ProcessEnvironmentVariableScope("MOSQUITTO_SERVICE_HOST", null);
        using ProcessEnvironmentVariableScope mosquittoPortScope = new ProcessEnvironmentVariableScope("MOSQUITTO_SERVICE_PORT", null);

        MqttFactory factory = new MqttFactory();
        using IMqttClient client = factory.CreateMqttClient();
        TaskCompletionSource<string> receivedPayload = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        client.ApplicationMessageReceivedAsync += args =>
        {
            if (args.ApplicationMessage.Topic == "manoir/mesh/entities/demo-entity/Mode")
            {
                receivedPayload.TrySetResult(Encoding.UTF8.GetString(args.ApplicationMessage.Payload ?? Array.Empty<byte>()));
            }

            return Task.CompletedTask;
        };

        await client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithClientId("functional-mqtt-entity-subscriber")
            .WithTcpServer(host.Host, host.Port)
            .Build());
        await client.SubscribeAsync("manoir/mesh/entities/demo-entity/Mode");

        try
        {
            MqttHelper.Start("functional-tests");
            MqttHelper.PublishEntity(new Entity()
            {
                Id = "demo-entity",
                EntityKind = "core:status",
                Name = "Demo",
                Datas =
                {
                    ["Mode"] = new EntityData()
                    {
                        SimpleType = "System.String",
                        SimpleValue = "Away"
                    }
                }
            });

            string payload = await receivedPayload.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.AreEqual("Away", payload);
        }
        finally
        {
            MqttHelper.Stop();
            await client.DisconnectAsync();
        }
    }
}