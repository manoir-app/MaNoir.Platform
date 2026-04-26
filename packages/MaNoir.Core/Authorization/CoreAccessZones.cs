using MaNoir.Core.Contracts.Models.Authorization;
using System.Collections.Generic;

namespace MaNoir.Core.Authorization;

/// <summary>
/// Exposes the built-in Core access zones.
/// </summary>
public static class CoreAccessZones
{
    /// <summary>
    /// Grants access to the Core Admin UI contributions.
    /// </summary>
    public const string CoreAdminUi = "core.admin-ui";

    /// <summary>
    /// Grants access to user authorization management.
    /// </summary>
    public const string CoreAuthorization = "core.authorization";

    /// <summary>
    /// Gets the built-in access zone definitions published by Core.
    /// </summary>
    public static IReadOnlyCollection<AccessZoneDefinition> GetDefinitions()
    {
        return
        [
            new AccessZoneDefinition()
            {
                Id = CoreAdminUi,
                Label = "Core Admin UI",
                Description = "Access to the Core administration user interface."
            },
            new AccessZoneDefinition()
            {
                Id = CoreAuthorization,
                Label = "Core Authorization",
                Description = "Manage Core user grants and admin delegation."
            }
        ];
    }
}