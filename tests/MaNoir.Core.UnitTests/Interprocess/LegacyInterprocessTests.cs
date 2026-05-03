using Home.Common;
using Home.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace MaNoir.Core.UnitTests.Interprocess;

[TestClass]
public sealed class LegacyInterprocessTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void BaseMessageHelpers_ShouldRoundTripLegacyTopicAndPayload()
    {
        MeshStatusChangeMessage message = new MeshStatusChangeMessage()
        {
            MeshId = "local",
            StatusKind = "privacy",
            NewStatus = "enabled"
        };

        string json = JsonConvert.SerializeObject(message);
        string topic = BaseMessage.GetTopic(json);
        MeshStatusChangeMessage roundTripped = BaseMessage.ReadAs<MeshStatusChangeMessage>(json);

        Assert.AreEqual(new MeshStatusChangeMessage().Topic, topic);
        Assert.AreEqual("local", roundTripped.MeshId);
        Assert.AreEqual("privacy", roundTripped.StatusKind);
        Assert.AreEqual("enabled", roundTripped.NewStatus);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void LegacyMessages_ShouldKeepHardcodedTopics()
    {
        Assert.AreEqual(MeshScenarioMessage.ChangedTopic, new MeshScenarioMessage().Topic);
        Assert.AreEqual(MeshScenarioMessage.SetTopic, new MeshScenarioMessage(MeshScenarioMessage.SetTopic).Topic);
        Assert.AreEqual("agent.test.topic", new AgentGenericMessage("agent.test.topic").Topic);
        Assert.AreEqual(MeshExtensionOperationMessage.TopicRestart, new MeshExtensionOperationMessage().Topic);
        Assert.AreEqual(MeshPublicBaseDomainChangedMessage.TopicName, new MeshPublicBaseDomainChangedMessage().Topic);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void GetServers_ShouldUseLegacyEnvironmentVariables()
    {
        string previousHost = Environment.GetEnvironmentVariable("NATS_SERVICE_HOST");
        string previousPort = Environment.GetEnvironmentVariable("NATS_SERVICE_PORT");
        string previousCompatPort = Environment.GetEnvironmentVariable("NATS_PORT_4222_TCP_PROTO");

        try
        {
            Environment.SetEnvironmentVariable("NATS_SERVICE_HOST", "nats.internal");
            Environment.SetEnvironmentVariable("NATS_SERVICE_PORT", "5333");
            Environment.SetEnvironmentVariable("NATS_PORT_4222_TCP_PROTO", null);

            string[] servers = NatsInterprocess.GetServers();

            CollectionAssert.AreEqual(new[] { "nats://nats.internal:5333" }, servers);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NATS_SERVICE_HOST", previousHost);
            Environment.SetEnvironmentVariable("NATS_SERVICE_PORT", previousPort);
            Environment.SetEnvironmentVariable("NATS_PORT_4222_TCP_PROTO", previousCompatPort);
        }
    }
}