using MaNoir.Core.Contracts.Models.Users;
using System.Collections.Generic;

namespace MaNoir.Core.Users;

/// <summary>
/// Describes presence transitions detected during one computation pass.
/// </summary>
public sealed class PresenceChangeSet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PresenceChangeSet"/> class.
    /// </summary>
    public PresenceChangeSet()
    {
        NewlyAbsentUserIds = [];
        NewlyPresentUserIds = [];
    }

    /// <summary>
    /// Gets the updated user when a single-user operation changed presence data.
    /// </summary>
    public User UpdatedUser { get; set; }

    /// <summary>
    /// Gets the user identifiers that just became present.
    /// </summary>
    public List<string> NewlyPresentUserIds { get; set; }

    /// <summary>
    /// Gets the user identifiers that just became absent.
    /// </summary>
    public List<string> NewlyAbsentUserIds { get; set; }

    /// <summary>
    /// Gets whether at least one presence transition was detected.
    /// </summary>
    public bool HasChanges => NewlyPresentUserIds.Count > 0 || NewlyAbsentUserIds.Count > 0;
}