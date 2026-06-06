using MaNoir.CommunicationHub.Contracts.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MaNoir.CommunicationHub.Chat;

/// <summary>
/// Provides business logic for Communication Hub chat surfaces.
/// </summary>
public sealed partial class CommunicationChatLogic
{
    private readonly CommunicationChatMongoOperations _mongoOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationChatLogic"/> class.
    /// </summary>
    public CommunicationChatLogic()
    {
        _mongoOperations = new CommunicationChatMongoOperations();
    }

    /// <summary>
    /// Normalizes a channel identifier.
    /// </summary>
    public static string NormalizeChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            return null;

        return channelId.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a participant identifier.
    /// </summary>
    public static string NormalizeParticipantId(string participantId)
    {
        if (string.IsNullOrWhiteSpace(participantId))
            return null;

        return participantId.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Prepares a channel aggregate for persistence.
    /// </summary>
    public static void PrepareChannelForSave(CommunicationChannel channel)
    {
        if (channel == null)
            return;

        if (string.IsNullOrWhiteSpace(channel.Id))
            channel.Id = Guid.NewGuid().ToString("D").ToLowerInvariant();
        else
            channel.Id = NormalizeChannelId(channel.Id);

        if (channel.Participants == null || channel.Participants.Count == 0)
            throw new InvalidCommunicationChannelException("A communication channel must contain at least one participant.");

        List<CommunicationParticipant> normalizedParticipants = [];
        HashSet<string> seenParticipantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (CommunicationParticipant participant in channel.Participants)
        {
            if (participant == null)
                continue;

            string normalizedParticipantId = NormalizeParticipantId(participant.Id);
            if (normalizedParticipantId == null)
                throw new InvalidCommunicationChannelException("A communication channel participant identifier cannot be empty.");

            if (!seenParticipantIds.Add(normalizedParticipantId))
                continue;

            participant.Id = normalizedParticipantId;
            participant.DisplayName = participant.DisplayName?.Trim();
            normalizedParticipants.Add(participant);
        }

        if (normalizedParticipants.Count == 0)
            throw new InvalidCommunicationChannelException("A communication channel must contain at least one valid participant.");

        channel.Label = channel.Label?.Trim();
        channel.Participants = normalizedParticipants;
    }

    /// <summary>
    /// Prepares a message aggregate for persistence.
    /// </summary>
    public static void PrepareMessageForSave(CommunicationMessage message)
    {
        if (message == null)
            return;

        message.ChannelId = NormalizeChannelId(message.ChannelId);
        message.SenderParticipantId = NormalizeParticipantId(message.SenderParticipantId);

        if (message.ChannelId == null)
            throw new InvalidCommunicationMessageException("A communication message channel identifier cannot be empty.");

        if (message.SenderParticipantId == null)
            throw new InvalidCommunicationMessageException("A communication message sender identifier cannot be empty.");

        if (message.Parts == null)
            message.Parts = [];

        foreach (CommunicationMessagePart part in message.Parts)
        {
            if (part == null)
                continue;

            part.Text = part.Text?.Trim();
            part.MimeType = part.MimeType?.Trim();
            part.Url = part.Url?.Trim();
            part.FileName = part.FileName?.Trim();
            part.PayloadJson = part.PayloadJson?.Trim();
        }

        if (!HasMeaningfulContent(message))
            throw new InvalidCommunicationMessageException("A communication message must contain at least one non-empty part or preview text.");

        message.PreviewText = BuildPreviewText(message);
    }

    private static bool HasMeaningfulContent(CommunicationMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.PreviewText))
            return true;

        return message.Parts.Any(part => part != null &&
            (!string.IsNullOrWhiteSpace(part.Text)
            || !string.IsNullOrWhiteSpace(part.PayloadJson)
            || !string.IsNullOrWhiteSpace(part.Url)
            || !string.IsNullOrWhiteSpace(part.FileName)));
    }

    private static string BuildPreviewText(CommunicationMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.PreviewText))
            return message.PreviewText.Trim();

        CommunicationMessagePart firstTextPart = message.Parts.FirstOrDefault(part => part != null && !string.IsNullOrWhiteSpace(part.Text));
        if (firstTextPart != null)
        {
            string preview = firstTextPart.Text.Trim();
            if (preview.Length > 160)
                return preview.Substring(0, 160);

            return preview;
        }

        CommunicationMessagePart firstReferencePart = message.Parts.FirstOrDefault(part => part != null && !string.IsNullOrWhiteSpace(part.FileName));
        if (firstReferencePart != null)
            return firstReferencePart.FileName.Trim();

        foreach (CommunicationMessagePart part in message.Parts)
        {
            string structuredPreview = TryBuildPreviewFromStructuredPayload(part);
            if (!string.IsNullOrWhiteSpace(structuredPreview))
                return structuredPreview;
        }

        return message.Kind.ToString();
    }

    private static string TryBuildPreviewFromStructuredPayload(CommunicationMessagePart part)
    {
        if (part == null || part.Kind != CommunicationMessagePartKind.StructuredPayload || string.IsNullOrWhiteSpace(part.PayloadJson))
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(part.PayloadJson);
            JsonElement root = document.RootElement;

            if (part.MimeType == CommunicationPayloadMimeTypes.Card)
                return ReadFirstNonEmptyProperty(root, "title", "summary", "bodyMarkdown");

            if (part.MimeType == CommunicationPayloadMimeTypes.Attachment)
                return ReadFirstNonEmptyProperty(root, "fileName", "attachmentId", "url");

            if (part.MimeType == CommunicationPayloadMimeTypes.SystemEvent)
                return ReadFirstNonEmptyProperty(root, "summary", "eventKind", "relatedEntityId");

            return ReadFirstNonEmptyProperty(root, "title", "summary", "label", "fileName");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ReadFirstNonEmptyProperty(JsonElement root, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (root.TryGetProperty(propertyName, out JsonElement property)
                && property.ValueKind == JsonValueKind.String)
            {
                string value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }

        return null;
    }
}