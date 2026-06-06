using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Home.Common;
using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Agents;
using MaNoir.Core.Users;

namespace MaNoir.Agents.Erza;

public sealed class ErzaRuntime
{
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
        LogInfo($"Starting agent {AgentId} for mesh {MeshId}.");
    }

    public void ReportHeartbeat()
    {
        LogInfo($"Heartbeat emitted by agent {AgentId} for mesh {MeshId}.");
    }

    public void ReportRegistrationSucceeded(RegisteredAgent agent)
    {
        LogInfo($"Agent {agent.AgentId} registered in mesh {agent.MeshId} with state {agent.State}.");
    }

    public void ReportRegistrationFailed(Exception exception)
    {
        LogWarning($"Agent {AgentId} could not register in mesh {MeshId}.", exception);
    }

    public void ReportHeartbeatFailed(Exception exception)
    {
        LogWarning($"Heartbeat failed for agent {AgentId} in mesh {MeshId}.", exception);
    }

    public void ReportStopping()
    {
        LogInfo($"Stopping agent {AgentId}.");
    }

    public void ReportTopicsSubscribed()
    {
        LogInfo($"Agent {AgentId} subscribed to topics: {string.Join(", ", MessageTopics)}.");
    }

    public void ReportInterprocessStopped()
    {
        LogInfo($"Interprocess listener stopped for agent {AgentId}.");
    }

    public void RunPresenceMaintenance()
    {
        LogInfo($"Presence maintenance iteration executed by {AgentId}.");
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

        LogInfo($"Presence changed for users: {string.Join(", ", changeSet.NewlyPresentUserIds.Concat(changeSet.NewlyAbsentUserIds).Distinct(StringComparer.OrdinalIgnoreCase))}.");
    }

    public void ReportNetworkConnectivityRefresh(InternetConnection connection, bool meshStatusChanged)
    {
        if (connection == null)
            return;

        LogInfo($"Network connectivity refreshed for {connection.Id} with status {connection.Status}. Mesh status changed: {meshStatusChanged}. Message: {connection.LastMessage}");
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

    private static void LogInfo(string message)
    {
        Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] [Erza] {message}");
    }

    private static void LogWarning(string message, Exception exception)
    {
        Console.WriteLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] [Erza] WARNING {message} {exception.Message}");
    }
}