namespace MaNoir.Core.Mesh;

public sealed class AutomationMeshBusinessRules
{
    // TODO: Migrate mesh aggregate loading and bootstrap logic from:
    // - old/HomeGraph/Home.Graph.Server/Controllers/AutomationMeshController.cs
    // - old/HomeGraph/Home.Graph.Server/Controllers/AutomationMeshController-General.cs
    // This includes local mesh creation, public id initialization, and main server defaults.

    // TODO: Migrate source code integration and account association rules.
    // Legacy surface:
    // - POST local/source-code-integration
    // - GET  local/associate-account/{accountGuid}

    // TODO: Migrate mesh settings rules.
    // Legacy surface:
    // - GET  {name}/location
    // - POST {name}/settings
    // - POST {name}/location
    // - GET  {name}/location/set/{locationId}
    // - GET  settings/available/language
    // - GET  settings/available/timezone
    // Business decisions to preserve:
    // - normalization to the local mesh
    // - language/timezone validation
    // - side effects on dependent agents when settings change

    // TODO: Migrate privacy mode rules.
    // Legacy surface:
    // - GET local/privacymode/set
    // - GET local/privacymode/clear
    // - GET local/privacymode/isenabled
    // Preserve side effects currently emitted to messaging and realtime hubs.

    // TODO: Migrate global scenario rules.
    // Legacy surface:
    // - GET/POST/DELETE local/globalscenario/*
    // Includes create/update/remove scenario, image management, and current scenario selection.

    // TODO: Migrate mesh extension lifecycle rules.
    // Legacy surface:
    // - GET/POST/DELETE local/extensions
    // - GET local/extensions/{extensionId}/restart
    // - GET local/extensions/{extensionId}/install
    // - GET local/extensions/{extensionId}/uninstall
    // - GET local/extensions/{extensionId}/setinstalled

    // TODO: Migrate integration configuration rules.
    // Legacy surface:
    // - GET local/integrations
    // - GET local/integrations/byagent/{agentId}
    // - POST local/integrations
    // - POST local/integrations/raw
    // - GET/POST local/integrations/{integrationId}/config/{instanceId?}

    // TODO: Migrate internet connection status rules.
    // Legacy surface:
    // - GET/POST local/internet-connections

    // TODO: Migrate location information enrichment rules.
    // Legacy surface:
    // - POST local/location/infos/weatherhazard
    // - POST local/location/infos/weather

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