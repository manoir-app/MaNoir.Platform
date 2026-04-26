using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents a custom user data entry.
/// </summary>
public sealed class UserCustomData
{
    /// <summary>
    /// Gets or sets the entry identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the custom data code.
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Gets or sets the related user identifier.
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// Gets or sets the encrypted payload.
    /// </summary>
    public string EncryptedData { get; set; }
    /// <summary>
    /// Gets or sets the encryption mode.
    /// </summary>
    public string EncryptionMode { get; set; }
    /// <summary>
    /// Gets or sets the additional custom properties.
    /// </summary>
    public Dictionary<string, string> PropertyBag { get; set; }
}