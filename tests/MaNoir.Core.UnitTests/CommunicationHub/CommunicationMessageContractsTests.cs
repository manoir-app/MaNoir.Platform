using MaNoir.CommunicationHub.Contracts.Models.Chat;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace MaNoir.Core.UnitTests.CommunicationHub;

[TestClass]
public sealed class CommunicationMessageContractsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void CommunicationMessage_ShouldRoundTripStructuredAndMarkdownParts()
    {
        CommunicationMessage message = new CommunicationMessage()
        {
            Id = "message-01",
            ChannelId = "channel-01",
            SenderParticipantId = "sarah",
            Kind = CommunicationMessageKind.Standard,
            SentAt = new DateTimeOffset(2026, 05, 17, 10, 30, 00, TimeSpan.Zero),
            PreviewText = "Inspection demandee",
            Parts =
            [
                new CommunicationMessagePart()
                {
                    Kind = CommunicationMessagePartKind.Markdown,
                    Text = "## Inspection\nLe filtre doit etre change.",
                    MimeType = "text/markdown",
                },
                new CommunicationMessagePart()
                {
                    Kind = CommunicationMessagePartKind.StructuredPayload,
                    MimeType = "application/vnd.manoir.task-card+json",
                    PayloadJson = "{\"taskId\":\"task-42\",\"priority\":\"high\"}",
                },
            ],
        };

        string json = JsonSerializer.Serialize(message);
        CommunicationMessage roundTripped = JsonSerializer.Deserialize<CommunicationMessage>(json);

        Assert.IsNotNull(roundTripped);
        Assert.AreEqual("message-01", roundTripped.Id);
        Assert.AreEqual(2, roundTripped.Parts.Count);
        Assert.AreEqual(CommunicationMessagePartKind.Markdown, roundTripped.Parts[0].Kind);
        Assert.AreEqual("text/markdown", roundTripped.Parts[0].MimeType);
        Assert.AreEqual(CommunicationMessagePartKind.StructuredPayload, roundTripped.Parts[1].Kind);
        Assert.AreEqual("application/vnd.manoir.task-card+json", roundTripped.Parts[1].MimeType);
        Assert.AreEqual("Inspection demandee", roundTripped.PreviewText);
        StringAssert.Contains(json, "StructuredPayload");
        StringAssert.Contains(json, "Markdown");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void CommunicationPayloadConventions_ShouldExposeRichPayloadMimeTypes()
    {
        CommunicationCardPayload payload = new CommunicationCardPayload()
        {
            Title = "Filtre a remplacer",
            Summary = "Maintenance cuisine",
            BodyMarkdown = "Le filtre principal doit etre change avant vendredi.",
            Actions =
            [
                new CommunicationCardAction()
                {
                    Id = "open-task",
                    Label = "Ouvrir la tache",
                    Kind = CommunicationCardActionKind.OpenUrl,
                    Target = "/tasks/task-42",
                },
            ],
        };

        string payloadJson = JsonSerializer.Serialize(payload);

        Assert.AreEqual("application/vnd.manoir.communication.card+json", CommunicationPayloadMimeTypes.Card);
        Assert.AreEqual("application/vnd.manoir.communication.attachment+json", CommunicationPayloadMimeTypes.Attachment);
        Assert.AreEqual("application/vnd.manoir.communication.system-event+json", CommunicationPayloadMimeTypes.SystemEvent);
        StringAssert.Contains(payloadJson, "Filtre a remplacer");
        StringAssert.Contains(payloadJson, "open-task");
    }
}