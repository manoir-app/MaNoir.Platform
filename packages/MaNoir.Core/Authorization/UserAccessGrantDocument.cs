using MaNoir.Core.Contracts.Models.Authorization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MaNoir.Core.Authorization;

internal sealed class UserAccessGrantDocument
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }

    public string ZoneId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public AccessLevel Level { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}