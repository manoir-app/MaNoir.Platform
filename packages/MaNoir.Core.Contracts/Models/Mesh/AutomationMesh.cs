using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Mesh;

public enum AutomationMeshPrivacyMode
{
    Medium,
    High
}

public sealed class AutomationMesh
{
    public AutomationMesh()
    {
        InternetConnections = [];
        Status = new AutomationMeshStatus();
        LocationInfo = new AutomationMeshLocationInfo();
        Scenarios = [];
    }

    [BsonId]
    public string Id { get; set; }

    public string PublicId { get; set; }
    public string LocationId { get; set; }
    public AutomationMeshStatus Status { get; set; }
    public AutomationServer MainServer { get; set; }
    public List<InternetConnection> InternetConnections { get; set; }
    public List<AutomationMeshGlobalScenario> Scenarios { get; set; }
    public string CurrentScenario { get; set; }
    public string MainSsid { get; set; }
    public AutomationMeshLocationInfo LocationInfo { get; set; }
    public AutomationMeshPrivacyMode? CurrentPrivacyMode { get; set; }
    public AutomationMeshManoirAppAccount ManoirAppAccount { get; set; }
    public AutomationMeshSouceCodeIntegration SourceCodeIntegration { get; set; }
    public string TimeZoneId { get; set; }
    public string LanguageId { get; set; }
    public string CountryId { get; set; }
}

public sealed class AutomationMeshManoirAppAccount
{
    public Guid AccountGuid { get; set; }
    public string Name { get; set; }
    public string DomainPrefix { get; set; }
}

public sealed class AutomationMeshLocationInfo
{
    public AutomationMeshLocationInfo()
    {
        Weather = [];
        WeatherHazards = [];
    }

    public List<WeatherInfo> Weather { get; set; }
    public List<WeatherHazard> WeatherHazards { get; set; }
}

public sealed class AutomationMeshGlobalScenario
{
    public AutomationMeshGlobalScenario()
    {
        Images = [];
    }

    public string Code { get; set; }
    public string Label { get; set; }
    public Dictionary<string, string> Images { get; set; }
}

public sealed class AutomationMeshStatus
{
    public const string StatusOK = "ok";
    public const string StatusPartiallyOK = "ok-partial";
    public const string StatusKO = "ko";

    public string GeneralStatusCode { get; set; } = StatusOK;
    public string InternetConnectionStatusCode { get; set; } = StatusOK;
}

public sealed class AutomationServer
{
    public AutomationServer()
    {
        SecondaryRoles = [];
        MainRole = new AutomationServerRole();
    }

    public string Name { get; set; }
    public string Id { get; set; }
    public AutomationServerRole MainRole { get; set; }
    public List<AutomationServerRole> SecondaryRoles { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AutomationRole
{
    GraphApi
}

public sealed class AutomationServerRole
{
    public AutomationRole Role { get; set; }
    public CommunicationMode CommunicationMode { get; set; }
    public string Uri { get; set; }
}

public sealed class AutomationMeshSouceCodeIntegration
{
    public string GitRepoKind { get; set; }
    public string WebhookNotificationPrefix { get; set; }
    public string GitRepoUrl { get; set; }
    public string GitUsername { get; set; }
    public string GitPassword { get; set; }
    public string GitBranch { get; set; }
}