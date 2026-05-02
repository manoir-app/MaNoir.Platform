using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Home.Common;
using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Agents;
using MaNoir.Core.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MaNoir.Agents.Erza;

public sealed class ErzaRuntime
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ErzaRuntime> _logger;

    public ErzaRuntime(IConfiguration configuration, ILogger<ErzaRuntime> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string AgentId => GetConfiguredValue("Erza:AgentId", "ERZA_AGENT_ID", "erza");

    public string MeshId => GetConfiguredValue("Erza:MeshId", "MANOIR_MESH_ID", "local");

    public string DisplayName => GetConfiguredValue("Erza:DisplayName", "ERZA_DISPLAY_NAME", "Erza");

    public string LocalLocationId => GetConfiguredValue("Erza:LocalLocationId", "MANOIR_LOCAL_LOCATION_ID", null);

    public string MachineName => Environment.MachineName;

    public string GraphApiBaseUri => GetConfiguredValue("Erza:GraphApiBaseUri", "MANOIR_GRAPH_API_BASE_URI", "http://localhost");

    public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

    public TimeSpan HeartbeatInterval => GetConfiguredDuration("Erza:HeartbeatIntervalSeconds", "ERZA_HEARTBEAT_INTERVAL_SECONDS", 30);

    public TimeSpan PresenceMaintenanceInterval => GetConfiguredDuration("Erza:PresenceMaintenanceIntervalSeconds", "ERZA_PRESENCE_MAINTENANCE_INTERVAL_SECONDS", 120);

    public TimeSpan NetworkConnectivityInterval => GetConfiguredDuration("Erza:NetworkConnectivityIntervalSeconds", "ERZA_NETWORK_CONNECTIVITY_INTERVAL_SECONDS", 30);

    public string NetworkConnectionId => GetConfiguredValue("Erza:NetworkConnectionId", "ERZA_NETWORK_CONNECTION_ID", "wan-main");

    public string NetworkConnectionType => GetConfiguredValue("Erza:NetworkConnectionType", "ERZA_NETWORK_CONNECTION_TYPE", "internet");

    public string[] NetworkProbeUrls => GetConfiguredValue(
            "Erza:NetworkProbeUrls",
            "ERZA_NETWORK_PROBE_URLS",
            "https://www.google.com/,https://1.1.1.1/")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .ToArray();

    public string[] MessageTopics => GetConfiguredValue("Erza:Topics", "ERZA_TOPICS", "users.presence.*,system.mesh.*")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public List<string> Capabilities => GetConfiguredValue("Erza:Capabilities", "ERZA_CAPABILITIES", "presence,mesh.monitoring")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(capability => capability.Trim().ToLowerInvariant())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(capability => capability, StringComparer.OrdinalIgnoreCase)
        .ToList();

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

        _logger.LogInformation("Presence changed for users: {Users}.", string.Join(", ", changeSet.NewlyPresentUserIds.Concat(changeSet.NewlyAbsentUserIds).Distinct(StringComparer.OrdinalIgnoreCase)));
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

    private string GetConfiguredValue(string configurationKey, string environmentVariableName, string defaultValue)
    {
        string configuredValue = _configuration[configurationKey];
        if (!string.IsNullOrWhiteSpace(configuredValue))
            return configuredValue;

        configuredValue = Environment.GetEnvironmentVariable(environmentVariableName);
        return string.IsNullOrWhiteSpace(configuredValue) ? defaultValue : configuredValue;
    }

    private TimeSpan GetConfiguredDuration(string configurationKey, string environmentVariableName, int defaultSeconds)
    {
        string configuredValue = GetConfiguredValue(configurationKey, environmentVariableName, defaultSeconds.ToString());
        return int.TryParse(configuredValue, out int seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.FromSeconds(defaultSeconds);
    }
}