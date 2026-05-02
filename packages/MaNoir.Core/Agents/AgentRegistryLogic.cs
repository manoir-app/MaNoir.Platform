using MaNoir.Core.Contracts.Models.Agents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Agents;

/// <summary>
/// Registers and tracks runtime agents known by the platform.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// AgentRegistryLogic logic = new AgentRegistryLogic();
/// RegisteredAgent agent = await logic.RegisterAsync(new AgentRegistrationRequest()
/// {
///     MeshId = "local",
///     AgentId = "erza",
///     DisplayName = "Erza",
///     Capabilities = ["presence", "network-monitoring"]
/// }, cancellationToken);
///
/// agent = await logic.HeartbeatAsync(new AgentHeartbeatRequest()
/// {
///     MeshId = "local",
///     AgentId = "erza",
///     StatusMessage = "running"
/// }, cancellationToken);
/// </code>
/// </remarks>
public sealed class AgentRegistryLogic
{
    private readonly AgentRegistryMongoOperations _mongoOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRegistryLogic"/> class.
    /// </summary>
    public AgentRegistryLogic()
    {
        _mongoOperations = new AgentRegistryMongoOperations();
    }

    /// <remarks>
    /// <para>Use this method to list every known runtime agent for one mesh, or every mesh when <paramref name="meshId"/> is omitted.</para>
    /// </remarks>
    public Task<List<RegisteredAgent>> GetAgentsAsync(string meshId = null, CancellationToken cancellationToken = default)
    {
        string normalizedMeshId = NormalizeId(meshId);
        return _mongoOperations.GetAgentsAsync(normalizedMeshId, cancellationToken);
    }

    /// <remarks>
    /// <para>Example:</para>
    /// <code>
    /// AgentRegistryLogic logic = new AgentRegistryLogic();
    /// RegisteredAgent agent = await logic.GetAgentAsync("local", "erza", cancellationToken);
    /// </code>
    /// </remarks>
    public async Task<RegisteredAgent> GetAgentAsync(string meshId, string agentId, CancellationToken cancellationToken = default)
    {
        string normalizedMeshId = NormalizeId(meshId);
        string normalizedAgentId = NormalizeId(agentId);
        if (normalizedMeshId == null || normalizedAgentId == null)
            return null;

        return await _mongoOperations.GetAgentAsync(normalizedMeshId, normalizedAgentId, cancellationToken);
    }

    /// <remarks>
    /// <para>
    /// Registration is idempotent for one <c>meshId/agentId</c> pair: calling it again updates display metadata,
    /// capabilities, state, and heartbeat timestamps of the existing record.
    /// </para>
    /// </remarks>
    public async Task<RegisteredAgent> RegisterAsync(AgentRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        PreparedAgentRegistration prepared = PrepareRegistration(request);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        RegisteredAgent existingAgent = await _mongoOperations.GetAgentAsync(prepared.MeshId, prepared.AgentId, cancellationToken);
        RegisteredAgent agent = existingAgent ?? new RegisteredAgent()
        {
            Id = ComposeId(prepared.MeshId, prepared.AgentId),
            AgentId = prepared.AgentId,
            MeshId = prepared.MeshId,
            RegisteredAtUtc = now,
            Capabilities = []
        };

        agent.DisplayName = prepared.DisplayName;
        agent.Version = prepared.Version;
        agent.Capabilities = prepared.Capabilities;
        agent.State = prepared.State;
        agent.StatusMessage = prepared.StatusMessage;
        agent.LastHeartbeatUtc = now;
        agent.UpdatedAtUtc = now;

        await _mongoOperations.SaveAgentAsync(agent, cancellationToken);
        return agent;
    }

    /// <remarks>
    /// <para>
    /// Call this method periodically once the agent has been registered to keep its runtime status fresh.
    /// A missing registration raises a <see cref="KeyNotFoundException"/>.
    /// </para>
    /// </remarks>
    public async Task<RegisteredAgent> HeartbeatAsync(AgentHeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        PreparedAgentHeartbeat prepared = PrepareHeartbeat(request);
        RegisteredAgent agent = await _mongoOperations.GetAgentAsync(prepared.MeshId, prepared.AgentId, cancellationToken);
        if (agent == null)
            throw new KeyNotFoundException("The requested agent is not registered.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        agent.State = prepared.State;
        agent.StatusMessage = prepared.StatusMessage;
        agent.LastHeartbeatUtc = now;
        agent.UpdatedAtUtc = now;

        await _mongoOperations.SaveAgentAsync(agent, cancellationToken);
        return agent;
    }

    private static PreparedAgentRegistration PrepareRegistration(AgentRegistrationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        string meshId = NormalizeId(request.MeshId);
        string agentId = NormalizeId(request.AgentId);
        if (meshId == null)
            throw new ArgumentException("The mesh identifier cannot be empty.", nameof(request));

        if (agentId == null)
            throw new ArgumentException("The agent identifier cannot be empty.", nameof(request));

        return new PreparedAgentRegistration()
        {
            MeshId = meshId,
            AgentId = agentId,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.AgentId?.Trim() ?? agentId : request.DisplayName.Trim(),
            Version = NormalizeText(request.Version),
            Capabilities = PrepareCapabilities(request.Capabilities),
            State = request.State == AgentState.Unknown ? AgentState.Starting : request.State,
            StatusMessage = NormalizeText(request.StatusMessage)
        };
    }

    private static PreparedAgentHeartbeat PrepareHeartbeat(AgentHeartbeatRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        string meshId = NormalizeId(request.MeshId);
        string agentId = NormalizeId(request.AgentId);
        if (meshId == null)
            throw new ArgumentException("The mesh identifier cannot be empty.", nameof(request));

        if (agentId == null)
            throw new ArgumentException("The agent identifier cannot be empty.", nameof(request));

        return new PreparedAgentHeartbeat()
        {
            MeshId = meshId,
            AgentId = agentId,
            State = request.State == AgentState.Unknown ? AgentState.Ready : request.State,
            StatusMessage = NormalizeText(request.StatusMessage)
        };
    }

    private static List<string> PrepareCapabilities(IEnumerable<string> capabilities)
    {
        SortedSet<string> preparedCapabilities = new(StringComparer.OrdinalIgnoreCase);
        foreach (string capability in capabilities ?? [])
        {
            string normalizedCapability = NormalizeId(capability);
            if (normalizedCapability != null)
                preparedCapabilities.Add(normalizedCapability);
        }

        return [.. preparedCapabilities];
    }

    private static string ComposeId(string meshId, string agentId)
    {
        return string.Concat(meshId, ":", agentId);
    }

    private static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private sealed class PreparedAgentRegistration
    {
        public string MeshId { get; set; }
        public string AgentId { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public List<string> Capabilities { get; set; }
        public AgentState State { get; set; }
        public string StatusMessage { get; set; }
    }

    private sealed class PreparedAgentHeartbeat
    {
        public string MeshId { get; set; }
        public string AgentId { get; set; }
        public AgentState State { get; set; }
        public string StatusMessage { get; set; }
    }
}