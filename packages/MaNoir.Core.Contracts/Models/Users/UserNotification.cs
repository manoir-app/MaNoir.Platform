using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

public enum UserNotificationImportance
{
    Low,
    Info,
    Normal,
    High,
    Critical
}

public sealed class UserNotification
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }
    public UserNotificationImportance Importance { get; set; }
    public DateTimeOffset Date { get; set; }
    public string SourceAgent { get; set; }
    public string SourceAgentNotificationId { get; set; }
    public bool IsRead { get; set; }
    public string Category { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> CustomValues { get; set; }
}