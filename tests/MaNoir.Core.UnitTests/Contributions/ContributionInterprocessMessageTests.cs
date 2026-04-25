using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Contributions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;

namespace MaNoir.Core.UnitTests.Contributions;

[TestClass]
public sealed class ContributionInterprocessMessageTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void ContributionConfigurationMessage_ShouldKeepPluginScopedTopicAndPayload()
    {
        ContributionConfigurationMessage message = new ContributionConfigurationMessage("Sarah",
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue" },
            new ContributionInstance() { Id = "instance-01", ContributionDefinitionId = "sarah.hue", PluginId = "sarah" })
        {
            SetupValues = { ["bridgeIp"] = "192.168.1.20" }
        };

        string json = JsonConvert.SerializeObject(message);
        string topic = BaseMessage.GetTopic(json);
        ContributionConfigurationMessage roundTripped = BaseMessage.ReadAs<ContributionConfigurationMessage>(json);

        Assert.AreEqual("sarah.contribution.configure", topic);
        Assert.AreEqual("sarah.contribution.configure", roundTripped.Topic);
        Assert.AreEqual("sarah.hue", roundTripped.Contribution.Id);
        Assert.AreEqual("192.168.1.20", roundTripped.SetupValues["bridgeIp"]);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ContributionMessages_ShouldKeepHardcodedTopics()
    {
        Assert.AreEqual(PluginCatalogPublicationMessage.PublishTopic, new PluginCatalogPublicationMessage().Topic);
        Assert.AreEqual(ContributionDefinitionsChangedMessage.TopicName, new ContributionDefinitionsChangedMessage().Topic);
        Assert.AreEqual(ContributionInstancesChangedMessage.TopicName, new ContributionInstancesChangedMessage().Topic);
        Assert.AreEqual("sarah.contribution.secrets.resolve", new ContributionSecretsRequestMessage("sarah", "instance-01", "public-key").Topic);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ContributionConfigurationResponse_ShouldCloneSourceInstance()
    {
        ContributionConfigurationMessage source = new ContributionConfigurationMessage("sarah",
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue" },
            new ContributionInstance()
            {
                Id = "instance-01",
                ContributionDefinitionId = "sarah.hue",
                PluginId = "sarah",
                Label = "Hue"
            });

        ContributionConfigurationResponse response = new ContributionConfigurationResponse(source);

        Assert.IsNotNull(response.Instance);
        Assert.AreEqual("instance-01", response.Instance.Id);
        Assert.AreEqual("sarah.hue", response.Instance.ContributionDefinitionId);
        Assert.AreEqual("sarah", response.Instance.PluginId);
        Assert.AreEqual("Hue", response.Instance.Label);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ContributionSecretsRequestMessage_ShouldKeepPluginScopedTopicAndPayload()
    {
        using RSA rsa = RSA.Create(2048);
        string publicKeyPem = rsa.ExportRSAPublicKeyPem();

        ContributionSecretsRequestMessage message = new ContributionSecretsRequestMessage("Sarah", "instance-01", publicKeyPem);
        string json = JsonConvert.SerializeObject(message);
        ContributionSecretsRequestMessage roundTripped = BaseMessage.ReadAs<ContributionSecretsRequestMessage>(json);

        Assert.AreEqual("sarah.contribution.secrets.resolve", roundTripped.Topic);
        Assert.AreEqual("instance-01", roundTripped.InstanceId);
        Assert.AreEqual(publicKeyPem, roundTripped.PublicKeyPem);
    }
}