using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MaNoir.Core.Secrets;

/// <summary>
/// Represents one locally stored shared secret protected by the Core runtime.
/// </summary>
public sealed class SharedSecret
{
    /// <summary>
    /// Gets the current envelope format identifier.
    /// </summary>
    public const string EncryptionModeAes256GcmPbkdf2Sha256V1 = "aes-256-gcm/pbkdf2-sha256/v1";

    /// <summary>
    /// Gets or sets the canonical secret identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded encrypted payload.
    /// </summary>
    public string EncryptedData { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded AES-GCM nonce.
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded AES-GCM authentication tag.
    /// </summary>
    public string AuthenticationTag { get; set; }

    /// <summary>
    /// Gets or sets the envelope format identifier.
    /// </summary>
    public string EncryptionMode { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}