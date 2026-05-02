namespace MaNoir.Core.Contracts.Models.Agents;

/// <summary>
/// Describes the runtime state reported by one agent.
/// </summary>
public enum AgentState
{
    Unknown = 0,
    Starting = 1,
    Ready = 2,
    Degraded = 3,
    Stopping = 4,
    Stopped = 5
}