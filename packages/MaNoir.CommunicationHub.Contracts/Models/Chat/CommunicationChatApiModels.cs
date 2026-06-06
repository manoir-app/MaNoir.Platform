using System.Collections.Generic;

namespace MaNoir.CommunicationHub.Contracts.Models.Chat;

/// <summary>
/// Represents the API payload used to create or update a channel.
/// </summary>
public sealed class CommunicationChannelUpsertRequest
{
    /// <summary>
    /// Gets or sets the display label of the channel.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the kind of channel to persist.
    /// </summary>
    public CommunicationChannelKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the participants attached to the channel.
    /// </summary>
    public List<CommunicationParticipantUpsertRequest> Participants { get; set; } = [];

    /// <summary>
    /// Gets or sets channel metadata values.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents one participant entry supplied through the channel API.
/// </summary>
public sealed class CommunicationParticipantUpsertRequest
{
    /// <summary>
    /// Gets or sets the participant identifier.
    /// </summary>
    public string ParticipantId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the participant kind.
    /// </summary>
    public CommunicationParticipantKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the participant role.
    /// </summary>
    public CommunicationParticipantRole Role { get; set; }

    /// <summary>
    /// Gets or sets participant metadata values.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the API payload used to append one message to a channel.
/// </summary>
public sealed class CommunicationMessageAppendRequest
{
    /// <summary>
    /// Gets or sets the message kind.
    /// </summary>
    public CommunicationMessageKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the optional preview text.
    /// </summary>
    public string PreviewText { get; set; }

    /// <summary>
    /// Gets or sets the message parts supplied by the client.
    /// </summary>
    public List<CommunicationMessagePartRequest> Parts { get; set; } = [];

    /// <summary>
    /// Gets or sets the message metadata values.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents one message part supplied by the API caller.
/// </summary>
public sealed class CommunicationMessagePartRequest
{
    /// <summary>
    /// Gets or sets the message part kind.
    /// </summary>
    public CommunicationMessagePartKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the MIME type.
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the inline text.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the linked URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the attached file name.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the structured JSON payload.
    /// </summary>
    public string PayloadJson { get; set; }
}

/// <summary>
/// Represents one channel returned by the chat API.
/// </summary>
public sealed class CommunicationChannelResponse
{
    /// <summary>
    /// Gets or sets the channel identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the channel kind.
    /// </summary>
    public CommunicationChannelKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the participants attached to the channel.
    /// </summary>
    public List<CommunicationParticipantResponse> Participants { get; set; } = [];

    /// <summary>
    /// Gets or sets channel metadata values.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents one participant returned by the chat API.
/// </summary>
public sealed class CommunicationParticipantResponse
{
    /// <summary>
    /// Gets or sets the participant identifier.
    /// </summary>
    public string ParticipantId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the participant kind.
    /// </summary>
    public CommunicationParticipantKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the participant role.
    /// </summary>
    public CommunicationParticipantRole Role { get; set; }

    /// <summary>
    /// Gets or sets participant metadata values.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents one message returned by the chat API.
/// </summary>
public sealed class CommunicationMessageResponse
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the channel identifier.
    /// </summary>
    public string ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the sender participant identifier.
    /// </summary>
    public string SenderParticipantId { get; set; }

    /// <summary>
    /// Gets or sets the message kind.
    /// </summary>
    public CommunicationMessageKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the send timestamp.
    /// </summary>
    public string SentAt { get; set; }

    /// <summary>
    /// Gets or sets the preview text.
    /// </summary>
    public string PreviewText { get; set; }

    /// <summary>
    /// Gets or sets the returned message parts.
    /// </summary>
    public List<CommunicationMessagePartResponse> Parts { get; set; } = [];

    /// <summary>
    /// Gets or sets message metadata values.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents one message part returned by the chat API.
/// </summary>
public sealed class CommunicationMessagePartResponse
{
    /// <summary>
    /// Gets or sets the message part kind.
    /// </summary>
    public CommunicationMessagePartKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the MIME type.
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the inline text.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the linked URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the attached file name.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the structured JSON payload.
    /// </summary>
    public string PayloadJson { get; set; }
}