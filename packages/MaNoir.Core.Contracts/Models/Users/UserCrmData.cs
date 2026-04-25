using MongoDB.Bson.Serialization.Attributes;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents CRM-oriented information attached to a user relationship.
/// </summary>
public sealed class UserCrmData
{
    /// <summary>
    /// Gets or sets the entry identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the user owning the CRM entry.
    /// </summary>
    public string DataOwnerUserId { get; set; }
    /// <summary>
    /// Gets or sets the user targeted by the CRM entry.
    /// </summary>
    public string SubjectUserId { get; set; }
    /// <summary>
    /// Gets or sets the CRM summary payload.
    /// </summary>
    public string Summary { get; set; }
}