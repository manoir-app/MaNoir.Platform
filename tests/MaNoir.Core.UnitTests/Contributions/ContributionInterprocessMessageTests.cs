using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Contributions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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
}