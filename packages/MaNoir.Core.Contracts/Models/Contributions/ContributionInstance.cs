using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Contributions;

/// <summary>
/// Represents the lifecycle status of a contribution instance.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ContributionInstanceStatus
{
    NotConfigured,
    Functional,
    Error,
    IncompleteConfiguration,
    Archived
}

/// <summary>
/// Represents a configured instance of a contribution.
/// </summary>
public sealed class ContributionInstance
{
    public ContributionInstance()
    {
        Settings = [];
        IsEnabled = true;
        Status = ContributionInstanceStatus.NotConfigured;
    }

    /// <summary>
    /// Gets or sets the canonical instance identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the related contribution definition identifier.
    /// </summary>
    public string ContributionDefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the owning plugin identifier.
    /// </summary>
    public string PluginId { get; set; }

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the instance is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the instance is fully configured.
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Gets or sets per-instance settings.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the current operational status.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public ContributionInstanceStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the human-readable status message.
    /// </summary>
    public string StatusMessage { get; set; }
}