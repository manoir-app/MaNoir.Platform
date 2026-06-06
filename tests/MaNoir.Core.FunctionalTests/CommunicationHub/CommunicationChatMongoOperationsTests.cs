using MaNoir.CommunicationHub.Chat;
using MaNoir.CommunicationHub.Contracts.Models.Chat;
using MaNoir.Core.FunctionalTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.CommunicationHub;

[TestClass]
[DoNotParallelize]
public sealed class CommunicationChatMongoOperationsTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task SaveAndQueryAsync_ShouldPersistRichMessagePayloadsAndParticipantScopedChannels()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        CommunicationChatLogic logic = new CommunicationChatLogic();

        CommunicationChannel channel = new CommunicationChannel()
        {
            Id = "kitchen-maintenance",
            Label = "Cuisine",
            Kind = CommunicationChannelKind.Group,
            Participants =
            [
                new CommunicationParticipant() { Id = "sarah", DisplayName = "Sarah", Kind = CommunicationParticipantKind.Agent, Role = CommunicationParticipantRole.Member },
                new CommunicationParticipant() { Id = "michael", DisplayName = "Michael", Kind = CommunicationParticipantKind.User, Role = CommunicationParticipantRole.Owner },
            ],
        };

        CommunicationChannel storedChannel = await logic.UpsertChannelAsync(channel);

        CommunicationMessage stored = await logic.AppendMessageAsync(new CommunicationMessage()
        {
            ChannelId = channel.Id.ToUpperInvariant(),
            SenderParticipantId = "SARAH",
            Kind = CommunicationMessageKind.Standard,
            Parts =
            [
                new CommunicationMessagePart()
                {
                    Kind = CommunicationMessagePartKind.StructuredPayload,
                    MimeType = CommunicationPayloadMimeTypes.Card,
                    PayloadJson = "{\"title\":\"Filtre a remplacer\",\"summary\":\"Ventilation\"}",
                },
                new CommunicationMessagePart()
                {
                    Kind = CommunicationMessagePartKind.StructuredPayload,
                    MimeType = CommunicationPayloadMimeTypes.Attachment,
                    PayloadJson = "{\"attachmentId\":\"manual-01\",\"fileName\":\"notice.pdf\"}",
                },
            ],
        });

        System.Collections.Generic.List<CommunicationChannel> channels = await logic.GetChannelsForParticipantAsync("MICHAEL");
        System.Collections.Generic.List<CommunicationMessage> messages = await logic.GetMessagesAsync(channel.Id);

        Assert.AreEqual(1, channels.Count);
        Assert.IsNotNull(storedChannel);
        Assert.AreEqual("kitchen-maintenance", channels[0].Id);
        Assert.AreEqual("michael", storedChannel.Participants[1].Id);
        Assert.AreEqual(1, messages.Count);
        Assert.IsFalse(string.IsNullOrWhiteSpace(stored.Id));
        Assert.AreEqual("sarah", stored.SenderParticipantId);
        Assert.AreEqual("Filtre a remplacer", stored.PreviewText);
        Assert.AreEqual(2, messages[0].Parts.Count);
        Assert.AreEqual(CommunicationPayloadMimeTypes.Card, messages[0].Parts[0].MimeType);
        Assert.AreEqual(CommunicationPayloadMimeTypes.Attachment, messages[0].Parts[1].MimeType);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task AppendMessageAsync_ShouldRejectSenderOutsideChannel()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        CommunicationChatLogic logic = new CommunicationChatLogic();

        await logic.UpsertChannelAsync(new CommunicationChannel()
        {
            Id = "household",
            Label = "Household",
            Kind = CommunicationChannelKind.Group,
            Participants =
            [
                new CommunicationParticipant() { Id = "michael", DisplayName = "Michael", Kind = CommunicationParticipantKind.User, Role = CommunicationParticipantRole.Owner },
            ],
        });

        await Assert.ThrowsExceptionAsync<CommunicationParticipantNotInChannelException>(async () =>
        {
            await logic.AppendMessageAsync(new CommunicationMessage()
            {
                ChannelId = "HOUSEHOLD",
                SenderParticipantId = "sarah",
                Kind = CommunicationMessageKind.Standard,
                Parts =
                [
                    new CommunicationMessagePart()
                    {
                        Kind = CommunicationMessagePartKind.PlainText,
                        Text = "Bonjour",
                        MimeType = "text/plain",
                    },
                ],
            });
        });
    }
}