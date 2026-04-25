using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents the importance of a user notification.
/// </summary>
public enum UserNotificationImportance
{
    /// <summary>
    /// Low importance.
    /// </summary>
    Low,
    /// <summary>
    /// Informational importance.
    /// </summary>
    Info,
    /// <summary>
    /// Normal importance.
    /// </summary>
    Normal,
    /// <summary>
    /// High importance.
    /// </summary>
    High,
    /// <summary>
    /// Critical importance.
    /// </summary>
    Critical
}

/// <summary>
/// Represents a notification addressed to a user.
/// </summary>
public sealed class UserNotification
{
    /// <summary>
    /// Gets or sets the notification identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the target user identifier.
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// Gets or sets the notification importance.
    /// </summary>
    public UserNotificationImportance Importance { get; set; }
    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTimeOffset Date { get; set; }
    /// <summary>
    /// Gets or sets the source agent name.
    /// </summary>
    public string SourceAgent { get; set; }
    /// <summary>
    /// Gets or sets the source notification identifier.
    /// </summary>
    public string SourceAgentNotificationId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the notification has been read.
    /// </summary>
    public bool IsRead { get; set; }
    /// <summary>
    /// Gets or sets the notification category.
    /// </summary>
    public string Category { get; set; }
    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Gets or sets the notification description.
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Gets or sets implementation-specific custom values.
    /// </summary>
    public Dictionary<string, string> CustomValues { get; set; }
}