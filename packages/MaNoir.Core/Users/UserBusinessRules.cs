namespace MaNoir.Core.Users;

public sealed class UserBusinessRules
{
    // TODO: Migrate core user aggregate rules from:
    // - old/HomeGraph/Home.Graph.Server/Controllers/UsersController.cs
    // This includes:
    // - main user listing
    // - full user listing and lookup
    // - guest vs non-guest queries
    // - delete rules for main users and guests
    // - create/update rules for a user
    // - avatar update workflow
    // - identity and admin checks
    // - set main user status

    // TODO: Migrate guest identity generation and sanitization rules.
    // Legacy logic currently lives in UsersController helper methods:
    // - GetGuestUserId
    // - SanitizeGuestName
    // - Sanitize

    // TODO: Migrate password and pin hashing rules.
    // Legacy logic currently lives in:
    // - UsersController-Login.cs
    // - UsersController.cs
    // Preserve the current hashing compatibility first, then decide on future hardening.

    // TODO: Migrate login and logout business workflow.
    // Legacy surface:
    // - POST login
    // - GET logout
    // Separate what is domain behavior (state, notifications, device association) from transport/auth cookie wiring.

    // TODO: Migrate device login and device association workflow.
    // Legacy surface:
    // - POST login/device
    // Preserve:
    // - user/device association behavior
    // - device bootstrap when unknown
    // - mobile notification side effects
    // - API key token generation rules

    // TODO: Migrate presence rules.
    // Legacy surface:
    // - GET  presence/mesh/local/all
    // - POST presence/notifyactivity
    // - GET  presence/{userName}/forcelocation/{locationId}/{status}
    // - POST all/{userName}/presence
    // Preserve probability handling, cleanup windows, and presence-change side effects.

    // TODO: Migrate user notifications rules.
    // Legacy surface:
    // - GET  {user}/notifications/clearreaditems
    // - GET  {user}/notifications/markallasread
    // - GET  {user}/notifications
    // - GET  {user}/notifications/{notifId}/markasread
    // - POST {user}/notify
    // Decide later which mobile push side effects stay in Core versus an adapter layer.

    // TODO: Migrate custom data rules.
    // Legacy surface:
    // - GET/POST {user}/data/custom
    // - GET/DELETE {user}/data/custom/{dataCode}
    // - GET me/data/custom

    // TODO: Migrate interests rules.
    // Legacy surface:
    // - GET/POST me/interests
    // - GET/POST all/{user}/interests
    // - GET me/interests/{interestId}
    // - GET all/{user}/interests/{interestId}
    // - DELETE me/triggers/{interestId}
    // - DELETE {user}/interests/{interestId}

    // TODO: Migrate direct user interaction rules.
    // Legacy surface:
    // - GET interactions/greetings/fromdevice
    // - GET/POST interactions/greetings/all/{userName}
    // Reassess whether greeting generation belongs in Core or in a dedicated interaction service.
}