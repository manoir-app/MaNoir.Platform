namespace MaNoir.Core.Mesh;

public sealed partial class AutomationMeshLogic
{
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
}