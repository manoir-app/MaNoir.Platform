using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MaNoir.Core.Contracts.Models.Contributions;

/// <summary>
/// Represents the supported contribution kinds.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ContributionKind
{
    Unknown = 0,

    [EnumMember(Value = "integration")]
    Integration,

    [EnumMember(Value = "adminui.page")]
    AdminUiPage
}

/// <summary>
/// Represents a capability published by an installed plugin.
/// </summary>
public sealed class ContributionDefinition
{
    public ContributionDefinition()
    {
        Tags = [];
    }

    /// <summary>
    /// Gets or sets the canonical contribution identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the owning plugin identifier.
    /// </summary>
    public string PluginId { get; set; }

    /// <summary>
    /// Gets or sets the contribution kind.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public ContributionKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    public string ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the contribution should be hidden by default.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether instances can be created for this contribution.
    /// </summary>
    public bool CanCreateInstances { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether multiple instances can coexist.
    /// </summary>
    public bool CanInstallMultipleTimes { get; set; }

    /// <summary>
    /// Gets or sets contribution tags.
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the publication timestamp.
    /// </summary>
    public DateTimeOffset PublishedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets Integration specific contribution data.
    /// </summary>
    public IntegrationContributionDefinitionData Integration { get; set; }

    /// <summary>
    /// Gets or sets Admin UI specific contribution data.
    /// </summary>
    public AdminUiContributionDefinitionData AdminUi { get; set; }
}

/// <summary>
/// Describes where the integrated service runs.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum IntegrationServiceDependencyKind
{
    Local,
    Cloud
}

/// <summary>
/// Represents Integration specific contribution data.
/// </summary>
public sealed class IntegrationContributionDefinitionData
{
    public IntegrationContributionDefinitionData()
    {
        PublishedEntityKinds = [];
    }

    /// <summary>
    /// Gets or sets the domain where the integration is active.
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// Gets or sets the functional category within the domain.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Gets or sets the entity kinds published by this integration.
    /// </summary>
    public List<IntegrationPublishedEntityKindDefinition> PublishedEntityKinds { get; set; }

    /// <summary>
    /// Gets or sets where the integrated service runs.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public IntegrationServiceDependencyKind ServiceDependencyKind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the integration requires an external subscription.
    /// </summary>
    public bool RequiresExternalSubscription { get; set; }

    /// <summary>
    /// Gets or sets extra information about the external subscription requirement.
    /// </summary>
    public string ExternalSubscriptionInfo { get; set; }

    /// <summary>
    /// Gets or sets the documentation URL.
    /// </summary>
    public string DocumentationUrl { get; set; }
}

/// <summary>
/// Represents one entity kind published by an integration.
/// </summary>
public sealed class IntegrationPublishedEntityKindDefinition
{
    public IntegrationPublishedEntityKindDefinition()
    {
        Descriptions = [];
    }

    /// <summary>
    /// Gets or sets the entity kind identifier.
    /// </summary>
    public string Kind { get; set; }

    /// <summary>
    /// Gets or sets localized descriptions keyed by locale.
    /// </summary>
    public Dictionary<string, string> Descriptions { get; set; }
}

/// <summary>
/// Represents Admin UI specific contribution data.
/// </summary>
public sealed class AdminUiContributionDefinitionData
{
    public AdminUiContributionDefinitionData()
    {
        Pages = [];
    }

    /// <summary>
    /// Gets or sets the target domain.
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// Gets or sets the contributed pages.
    /// </summary>
    public List<AdminUiPageDefinition> Pages { get; set; }
}

/// <summary>
/// Represents one contributed Admin UI page.
/// </summary>
public sealed class AdminUiPageDefinition
{
    public AdminUiPageDefinition()
    {
        Labels = [];
    }

    /// <summary>
    /// Gets or sets the page category.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Gets or sets the stable page name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the page URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets localized labels keyed by locale.
    /// </summary>
    public Dictionary<string, string> Labels { get; set; }
}