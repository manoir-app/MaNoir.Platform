using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents the persisted presence information for a user.
/// </summary>
public sealed class PresenceData
{
    public PresenceData()
    {
        Location = [];
        LatestActivities = [];
    }

    /// <summary>
    /// Gets or sets the known location probabilities.
    /// </summary>
    public List<PresenceLocationData> Location { get; set; }
    /// <summary>
    /// Gets or sets the latest recorded presence activities.
    /// </summary>
    public List<PresenceActivityData> LatestActivities { get; set; }
}

/// <summary>
/// Represents a location probability entry for a user.
/// </summary>
public sealed class PresenceLocationData
{
    /// <summary>
    /// Gets or sets the location identifier.
    /// </summary>
    public string LocationId { get; set; }
    /// <summary>
    /// Gets or sets the confidence value for the location.
    /// </summary>
    public int Probability { get; set; }
    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset LatestUpdate { get; set; }
}

/// <summary>
/// Represents an activity that influences user presence.
/// </summary>
public class PresenceActivityData
{
    /// <summary>
    /// Gets or sets the activity timestamp.
    /// </summary>
    public DateTimeOffset? Date { get; set; }
    /// <summary>
    /// Gets or sets the emitting device identifier.
    /// </summary>
    public string DeviceId { get; set; }
    /// <summary>
    /// Gets or sets the related location identifier.
    /// </summary>
    public string LocationId { get; set; }
    /// <summary>
    /// Gets or sets the activity kind.
    /// </summary>
    public string ActivityKind { get; set; }
    /// <summary>
    /// Gets or sets the activity status.
    /// </summary>
    public string Status { get; set; }
}

/// <summary>
/// Represents a user-driven or system-driven presence notification.
/// </summary>
public sealed class PresenceNotificationData : PresenceActivityData
{
    /// <summary>
    /// Gets or sets the associated user identifier.
    /// </summary>
    public string AssociatedUser { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the notification came from explicit user input.
    /// </summary>
    public bool IsUserInput { get; set; }
}

/// <summary>
/// Represents an incremental update to a user's presence state.
/// </summary>
public sealed class PresenceUpdateData
{
    /// <summary>
    /// Gets or sets the location update to merge.
    /// </summary>
    public PresenceLocationData Location { get; set; }
    /// <summary>
    /// Gets or sets the activity entry to append.
    /// </summary>
    public PresenceActivityData ActivityToLog { get; set; }
}