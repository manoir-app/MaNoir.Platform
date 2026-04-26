using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MaNoir.Core.Contracts.Models.Authorization;

/// <summary>
/// Represents the supported access levels for one functional zone.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AccessLevel
{
    [EnumMember(Value = "none")]
    None = 0,

    [EnumMember(Value = "read")]
    Read = 1,

    [EnumMember(Value = "contribute")]
    Contribute = 2,

    [EnumMember(Value = "manage")]
    Manage = 3
}

/// <summary>
/// Represents one access level granted on a zone.
/// </summary>
public sealed class UserZoneAccess
{
    /// <summary>
    /// Gets or sets the stable zone identifier.
    /// </summary>
    public string ZoneId { get; set; }

    /// <summary>
    /// Gets or sets the granted access level.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public AccessLevel Level { get; set; }
}

/// <summary>
/// Represents the authorization profile of one user.
/// </summary>
public sealed class UserAuthorizationProfile
{
    public UserAuthorizationProfile()
    {
        Accesses = [];
    }

    /// <summary>
    /// Gets or sets the canonical user identifier.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is a main household user.
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is the current platform administrator.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Gets or sets the explicit zone accesses assigned to the user.
    /// </summary>
    public List<UserZoneAccess> Accesses { get; set; }
}

/// <summary>
/// Represents the payload used to replace the explicit zone accesses of a user.
/// </summary>
public sealed class UserAuthorizationUpdateRequest
{
    public UserAuthorizationUpdateRequest()
    {
        Accesses = [];
    }

    /// <summary>
    /// Gets or sets the explicit zone accesses to persist.
    /// </summary>
    public List<UserZoneAccess> Accesses { get; set; }
}

/// <summary>
/// Represents one published access zone definition.
/// </summary>
public sealed class AccessZoneDefinition
{
    /// <summary>
    /// Gets or sets the stable access zone identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the publishing plugin identifier.
    /// </summary>
    public string PluginId { get; set; }

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the first publication timestamp.
    /// </summary>
    public DateTimeOffset PublishedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}