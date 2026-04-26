namespace MaNoir.Core.Mesh;

public sealed partial class AutomationMeshLogic
{
    // TODO: Migrate trigger rules.
    // Legacy surface:
    // - GET/POST/DELETE local/triggers
    // - GET local/triggers/{triggerId}
    // - GET local/triggers/{triggerId}/settings
    // - GET/POST/AllowAnonymous local/triggers/{triggerId}/raise
    // Preserve domain behavior before deciding what remains in API or moves to integrations.

    // TODO: Migrate log retrieval rules if they are still considered part of platform core.
    // Legacy surface:
    // - local/logs
    // Reassess whether this belongs in Core or in a dedicated observability boundary.

    // TODO: Reassess agent registration and greetings endpoints.
    // Legacy surface:
    // - GET local/interactions/greetings/general
    // - GET local/agents
    // - POST local/agents/register
    // These may remain orchestration concerns rather than core business rules.
}