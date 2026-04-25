using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Contributions;

/// <summary>
/// Represents a plugin installed on the local platform instance.
/// </summary>
public sealed class InstalledPlugin
{
    public InstalledPlugin()
    {
        IsEnabled = true;
        IsHealthy = true;
        Contributions = [];
    }

    /// <summary>
    /// Gets or sets the canonical plugin identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets the plugin description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the plugin publisher.
    /// </summary>
    public string Publisher { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled locally.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin is considered healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the local installation timestamp.
    /// </summary>
    public DateTimeOffset InstalledAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last successful local heartbeat timestamp.
    /// </summary>
    public DateTimeOffset? LastSeenUtc { get; set; }

    /// <summary>
    /// Gets or sets the last catalog publication timestamp.
    /// </summary>
    public DateTimeOffset? LastPublishedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last published catalog fingerprint.
    /// </summary>
    public string LastPublishedCatalogFingerprint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin published a changed catalog.
    /// </summary>
    public bool HasNewFeatures { get; set; }

    /// <summary>
    /// Gets or sets the contributions published by this installed plugin.
    /// </summary>
    public List<ContributionDefinition> Contributions { get; set; }
}