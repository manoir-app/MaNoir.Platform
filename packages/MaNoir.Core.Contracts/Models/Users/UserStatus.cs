using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

public sealed class UserStatus
{
    public UserStatus()
    {
        ChatsStatus = [];
    }

    [BsonId]
    public string Id { get; set; }

    public Dictionary<string, UserStatusChat> ChatsStatus { get; set; }
}

public sealed class UserStatusChat
{
    public DateTimeOffset LastRead { get; set; }
}