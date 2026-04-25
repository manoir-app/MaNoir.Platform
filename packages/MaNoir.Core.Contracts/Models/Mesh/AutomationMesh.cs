using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Mesh;

/// <summary>
/// Represents the privacy mode currently applied to the local automation mesh.
/// </summary>
public enum AutomationMeshPrivacyMode
{
    /// <summary>
    /// Balanced privacy mode.
    /// </summary>
    Medium,
    /// <summary>
    /// Strict privacy mode.
    /// </summary>
    High
}

/// <summary>
/// Represents the main automation mesh aggregate.
/// </summary>
public sealed class AutomationMesh
{
    public AutomationMesh()
    {
        InternetConnections = [];
        Status = new AutomationMeshStatus();
        LocationInfo = new AutomationMeshLocationInfo();
        Scenarios = [];
    }

    /// <summary>
    /// Gets or sets the internal mesh identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the public mesh identifier.
    /// </summary>
    public string PublicId { get; set; }
    /// <summary>
    /// Gets or sets the related location identifier.
    /// </summary>
    public string LocationId { get; set; }
    /// <summary>
    /// Gets or sets the aggregated mesh status.
    /// </summary>
    public AutomationMeshStatus Status { get; set; }
    /// <summary>
    /// Gets or sets the main server description.
    /// </summary>
    public AutomationServer MainServer { get; set; }
    /// <summary>
    /// Gets or sets the configured internet connections.
    /// </summary>
    public List<InternetConnection> InternetConnections { get; set; }
    /// <summary>
    /// Gets or sets the global scenarios available on the mesh.
    /// </summary>
    public List<AutomationMeshGlobalScenario> Scenarios { get; set; }
    /// <summary>
    /// Gets or sets the current scenario code.
    /// </summary>
    public string CurrentScenario { get; set; }
    /// <summary>
    /// Gets or sets the primary Wi-Fi SSID.
    /// </summary>
    public string MainSsid { get; set; }
    /// <summary>
    /// Gets or sets the enriched location information.
    /// </summary>
    public AutomationMeshLocationInfo LocationInfo { get; set; }
    /// <summary>
    /// Gets or sets the currently enabled privacy mode.
    /// </summary>
    public AutomationMeshPrivacyMode? CurrentPrivacyMode { get; set; }
    /// <summary>
    /// Gets or sets the associated Manoir application account.
    /// </summary>
    public AutomationMeshManoirAppAccount ManoirAppAccount { get; set; }
    /// <summary>
    /// Gets or sets the source code integration settings.
    /// </summary>
    public AutomationMeshSouceCodeIntegration SourceCodeIntegration { get; set; }
    /// <summary>
    /// Gets or sets the mesh time zone identifier.
    /// </summary>
    public string TimeZoneId { get; set; }
    /// <summary>
    /// Gets or sets the mesh default language identifier.
    /// </summary>
    public string LanguageId { get; set; }
    /// <summary>
    /// Gets or sets the mesh country identifier.
    /// </summary>
    public string CountryId { get; set; }
}

/// <summary>
/// Represents the Manoir application account associated with a mesh.
/// </summary>
public sealed class AutomationMeshManoirAppAccount
{
    /// <summary>
    /// Gets or sets the account GUID.
    /// </summary>
    public Guid AccountGuid { get; set; }
    /// <summary>
    /// Gets or sets the account display name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the application domain prefix.
    /// </summary>
    public string DomainPrefix { get; set; }
}

/// <summary>
/// Represents location-related enrichment data for the mesh.
/// </summary>
public sealed class AutomationMeshLocationInfo
{
    public AutomationMeshLocationInfo()
    {
        Weather = [];
        WeatherHazards = [];
    }

    /// <summary>
    /// Gets or sets the weather entries for the mesh location.
    /// </summary>
    public List<WeatherInfo> Weather { get; set; }
    /// <summary>
    /// Gets or sets the weather hazards for the mesh location.
    /// </summary>
    public List<WeatherHazard> WeatherHazards { get; set; }
}

/// <summary>
/// Represents a global scenario available on the mesh.
/// </summary>
public sealed class AutomationMeshGlobalScenario
{
    public AutomationMeshGlobalScenario()
    {
        Images = [];
    }

    /// <summary>
    /// Gets or sets the scenario code.
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Gets or sets the scenario label.
    /// </summary>
    public string Label { get; set; }
    /// <summary>
    /// Gets or sets scenario images indexed by key.
    /// </summary>
    public Dictionary<string, string> Images { get; set; }
}

/// <summary>
/// Represents the high-level status of the mesh.
/// </summary>
public sealed class AutomationMeshStatus
{
    public const string StatusOK = "ok";
    public const string StatusPartiallyOK = "ok-partial";
    public const string StatusKO = "ko";

    /// <summary>
    /// Gets or sets the global status code.
    /// </summary>
    public string GeneralStatusCode { get; set; } = StatusOK;
    /// <summary>
    /// Gets or sets the internet connection status code.
    /// </summary>
    public string InternetConnectionStatusCode { get; set; } = StatusOK;
}

/// <summary>
/// Represents a server participating in the automation mesh.
/// </summary>
public sealed class AutomationServer
{
    public AutomationServer()
    {
        SecondaryRoles = [];
        MainRole = new AutomationServerRole();
    }

    /// <summary>
    /// Gets or sets the server display name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the server identifier.
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// Gets or sets the main role fulfilled by the server.
    /// </summary>
    public AutomationServerRole MainRole { get; set; }
    /// <summary>
    /// Gets or sets the additional roles fulfilled by the server.
    /// </summary>
    public List<AutomationServerRole> SecondaryRoles { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
/// <summary>
/// Identifies a functional role exposed by an automation server.
/// </summary>
public enum AutomationRole
{
    /// <summary>
    /// The server exposes the graph API role.
    /// </summary>
    GraphApi
}

/// <summary>
/// Represents a role exposed by an automation server.
/// </summary>
public sealed class AutomationServerRole
{
    /// <summary>
    /// Gets or sets the functional role.
    /// </summary>
    public AutomationRole Role { get; set; }
    /// <summary>
    /// Gets or sets the communication mode used to reach the role.
    /// </summary>
    public CommunicationMode CommunicationMode { get; set; }
    /// <summary>
    /// Gets or sets the role endpoint URI.
    /// </summary>
    public string Uri { get; set; }
}

/// <summary>
/// Represents source code integration settings for the mesh.
/// </summary>
public sealed class AutomationMeshSouceCodeIntegration
{
    /// <summary>
    /// Gets or sets the repository provider kind.
    /// </summary>
    public string GitRepoKind { get; set; }
    /// <summary>
    /// Gets or sets the webhook notification prefix.
    /// </summary>
    public string WebhookNotificationPrefix { get; set; }
    /// <summary>
    /// Gets or sets the repository URL.
    /// </summary>
    public string GitRepoUrl { get; set; }
    /// <summary>
    /// Gets or sets the Git username.
    /// </summary>
    public string GitUsername { get; set; }
    /// <summary>
    /// Gets or sets the Git password or token.
    /// </summary>
    public string GitPassword { get; set; }
    /// <summary>
    /// Gets or sets the tracked Git branch.
    /// </summary>
    public string GitBranch { get; set; }
}