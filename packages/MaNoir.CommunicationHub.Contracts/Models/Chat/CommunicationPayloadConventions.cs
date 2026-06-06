using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MaNoir.CommunicationHub.Contracts.Models.Chat;

/// <summary>
/// Defines the structured payload media types supported by the Communication Hub bootstrap.
/// </summary>
public static class CommunicationPayloadMimeTypes
{
    /// <summary>
    /// Gets the media type for card payloads.
    /// </summary>
    public const string Card = "application/vnd.manoir.communication.card+json";

    /// <summary>
    /// Gets the media type for attachment payloads.
    /// </summary>
    public const string Attachment = "application/vnd.manoir.communication.attachment+json";

    /// <summary>
    /// Gets the media type for system event payloads.
    /// </summary>
    public const string SystemEvent = "application/vnd.manoir.communication.system-event+json";
}

/// <summary>
/// Represents a portable rich card payload embedded in a communication message.
/// </summary>
public sealed class CommunicationCardPayload
{
    /// <summary>
    /// Gets or sets the main title of the card.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets a short summary line.
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    /// <summary>
    /// Gets or sets the markdown body of the card.
    /// </summary>
    [JsonPropertyName("bodyMarkdown")]
    public string BodyMarkdown { get; set; }

    /// <summary>
    /// Gets or sets the factual key-value pairs displayed by the card.
    /// </summary>
    [JsonPropertyName("facts")]
    public List<CommunicationCardFact> Facts { get; set; } = [];

    /// <summary>
    /// Gets or sets the actions available from the card.
    /// </summary>
    [JsonPropertyName("actions")]
    public List<CommunicationCardAction> Actions { get; set; } = [];
}

/// <summary>
/// Represents one factual line inside a card payload.
/// </summary>
public sealed class CommunicationCardFact
{
    /// <summary>
    /// Gets or sets the factual label.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the factual value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; }
}

/// <summary>
/// Represents an action exposed by a card payload.
/// </summary>
public sealed class CommunicationCardAction
{
    /// <summary>
    /// Gets or sets the stable action identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the action kind.
    /// </summary>
    [JsonPropertyName("kind")]
    public CommunicationCardActionKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the target URL, route, or command identifier.
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; }
}

/// <summary>
/// Represents an attachment payload embedded in a communication message.
/// </summary>
public sealed class CommunicationAttachmentPayload
{
    /// <summary>
    /// Gets or sets the attachment identifier.
    /// </summary>
    [JsonPropertyName("attachmentId")]
    public string AttachmentId { get; set; }

    /// <summary>
    /// Gets or sets the attachment file name.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the attachment MIME type.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the byte size when known.
    /// </summary>
    [JsonPropertyName("sizeInBytes")]
    public long? SizeInBytes { get; set; }

    /// <summary>
    /// Gets or sets the primary retrieval URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets an optional preview URL.
    /// </summary>
    [JsonPropertyName("previewUrl")]
    public string PreviewUrl { get; set; }
}

/// <summary>
/// Represents a system-level event payload emitted through the Communication Hub.
/// </summary>
public sealed class CommunicationSystemEventPayload
{
    /// <summary>
    /// Gets or sets the stable event kind.
    /// </summary>
    [JsonPropertyName("eventKind")]
    public string EventKind { get; set; }

    /// <summary>
    /// Gets or sets the short event summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    /// <summary>
    /// Gets or sets the event tone hint.
    /// </summary>
    [JsonPropertyName("tone")]
    public CommunicationSystemEventTone Tone { get; set; }

    /// <summary>
    /// Gets or sets the main correlation identifier.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the related entity kind when the event references one.
    /// </summary>
    [JsonPropertyName("relatedEntityKind")]
    public string RelatedEntityKind { get; set; }

    /// <summary>
    /// Gets or sets the related entity identifier when the event references one.
    /// </summary>
    [JsonPropertyName("relatedEntityId")]
    public string RelatedEntityId { get; set; }

    /// <summary>
    /// Gets or sets an optional structured event payload.
    /// </summary>
    [JsonPropertyName("detailJson")]
    public string DetailJson { get; set; }
}

/// <summary>
/// Describes the action semantic used by a card.
/// </summary>
public enum CommunicationCardActionKind
{
    /// <summary>
    /// Opens a URL or route.
    /// </summary>
    OpenUrl = 0,

    /// <summary>
    /// Triggers an inline client action.
    /// </summary>
    Invoke = 1,

    /// <summary>
    /// Requests an acknowledgement.
    /// </summary>
    Acknowledge = 2,
}

/// <summary>
/// Describes the tone of a system event.
/// </summary>
public enum CommunicationSystemEventTone
{
    /// <summary>
    /// Neutral informational event.
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// Positive event.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Warning event.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error event.
    /// </summary>
    Error = 3,
}