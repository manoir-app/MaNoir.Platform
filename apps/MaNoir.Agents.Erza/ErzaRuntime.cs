using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Home.Common;
using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Agents;
using MaNoir.Core.Users;
using Microsoft.Extensions.Logging;

namespace MaNoir.Agents.Erza;

public sealed class ErzaRuntime
{
    private readonly ILogger<ErzaRuntime> _logger;

    private static readonly string[] FixedMessageTopics = [
        "users.presence.*",
        "system.mesh.*",
        "system.auth.users.*"
    ];

    private static readonly string[] FixedCapabilities = [
        "presence",
        "mesh.monitoring",
        "security.monitoring"
    ];

    public ErzaRuntime(ILogger<ErzaRuntime> logger)
    {
        _logger = logger;
    }

    public string AgentId => "erza";

    public string MeshId => "local";

    public string DisplayName => "Erza";

    public string LocalLocationId => ResolveOptionalEnvironmentValue("MANOIR_LOCAL_LOCATION_ID");

    public string MachineName => Environment.MachineName;

    public string GraphApiBaseUri => ResolveEnvironmentValue("MANOIR_GRAPH_API_BASE_URI", "http://localhost:5243");

    public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

    public TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(30);

    public TimeSpan PresenceMaintenanceInterval => TimeSpan.FromSeconds(120);

    public TimeSpan NetworkConnectivityInterval => TimeSpan.FromSeconds(30);

    public string NetworkConnectionId => "wan-main";

    public string NetworkConnectionType => "internet";

    public string[] NetworkProbeUrls => [
        "https://www.google.com/",
        "https://1.1.1.1/"
    ];

    public string[] MessageTopics => [.. FixedMessageTopics];

    public List<string> Capabilities => [.. FixedCapabilities];

    public void ReportStarting()
    {
        _logger.LogInformation("Starting agent {AgentId} for mesh {MeshId}.", AgentId, MeshId);
    }

    public void ReportHeartbeat()
    {
        _logger.LogInformation("Heartbeat emitted by agent {AgentId} for mesh {MeshId}.", AgentId, MeshId);
    }

    public void ReportRegistrationSucceeded(RegisteredAgent agent)
    {
        _logger.LogInformation("Agent {AgentId} registered in mesh {MeshId} with state {State}.", agent.AgentId, agent.MeshId, agent.State);
    }

    public void ReportRegistrationFailed(Exception exception)
    {
        _logger.LogWarning(exception, "Agent {AgentId} could not register in mesh {MeshId}.", AgentId, MeshId);
    }

    public void ReportHeartbeatFailed(Exception exception)
    {
        _logger.LogWarning(exception, "Heartbeat failed for agent {AgentId} in mesh {MeshId}.", AgentId, MeshId);
    }

    public void ReportStopping()
    {
        _logger.LogInformation("Stopping agent {AgentId}.", AgentId);
    }

    public void ReportTopicsSubscribed()
    {
        _logger.LogInformation("Agent {AgentId} subscribed to topics: {Topics}.", AgentId, string.Join(", ", MessageTopics));
    }

    public void ReportInterprocessStopped()
    {
        _logger.LogInformation("Interprocess listener stopped for agent {AgentId}.", AgentId);
    }

    public void RunPresenceMaintenance()
    {
        _logger.LogInformation("Presence maintenance iteration executed by {AgentId}.", AgentId);
    }

    public void PublishPresenceChanges(PresenceChangeSet changeSet)
    {
        if (changeSet == null || !changeSet.HasChanges)
            return;

        foreach (string userId in changeSet.NewlyPresentUserIds.Concat(changeSet.NewlyAbsentUserIds).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            NatsInterprocess.Push(new PresenceChangedMessage()
            {
                UserId = userId
            });
        }

        _logger.LogInformation(
            "Presence changed for users: {UserIds}.",
            string.Join(", ", changeSet.NewlyPresentUserIds.Concat(changeSet.NewlyAbsentUserIds).Distinct(StringComparer.OrdinalIgnoreCase)));
    }

    public void ReportNetworkConnectivityRefresh(InternetConnection connection, bool meshStatusChanged)
    {
        if (connection == null)
            return;

        _logger.LogInformation(
            "Network connectivity refreshed for {ConnectionId} with status {Status}. Mesh status changed: {MeshStatusChanged}. Message: {Message}",
            connection.Id,
            connection.Status,
            meshStatusChanged,
            connection.LastMessage);
    }

    public AgentRegistrationRequest CreateRegistrationRequest(AgentState state, string statusMessage = null)
    {
        return new AgentRegistrationRequest()
        {
            AgentId = AgentId,
            DisplayName = DisplayName,
            MeshId = MeshId,
            Version = Version,
            Capabilities = Capabilities,
            State = state,
            StatusMessage = statusMessage
        };
    }

    public AgentHeartbeatRequest CreateHeartbeatRequest(AgentState state, string statusMessage = null)
    {
        return new AgentHeartbeatRequest()
        {
            AgentId = AgentId,
            MeshId = MeshId,
            State = state,
            StatusMessage = statusMessage
        };
    }

    private static string ResolveEnvironmentValue(string environmentVariableName, string defaultValue)
    {
        string configuredValue = Environment.GetEnvironmentVariable(environmentVariableName);
        return string.IsNullOrWhiteSpace(configuredValue) ? defaultValue : configuredValue;
    }

    private static string ResolveOptionalEnvironmentValue(string environmentVariableName)
    {
        string configuredValue = Environment.GetEnvironmentVariable(environmentVariableName);
        return string.IsNullOrWhiteSpace(configuredValue) ? null : configuredValue.Trim();
    }

}