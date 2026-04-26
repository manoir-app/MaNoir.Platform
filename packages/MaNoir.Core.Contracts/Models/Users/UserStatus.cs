using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents aggregated status information for a user.
/// </summary>
public sealed class UserStatus
{
    public UserStatus()
    {
        ChatsStatus = [];
    }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the chat statuses indexed by channel identifier.
    /// </summary>
    public Dictionary<string, UserStatusChat> ChatsStatus { get; set; }
}

/// <summary>
/// Represents the read state for a user chat channel.
/// </summary>
public sealed class UserStatusChat
{
    /// <summary>
    /// Gets or sets the last read timestamp.
    /// </summary>
    public DateTimeOffset LastRead { get; set; }
}