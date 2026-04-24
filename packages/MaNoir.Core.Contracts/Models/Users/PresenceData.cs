using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

public sealed class PresenceData
{
    public PresenceData()
    {
        Location = [];
        LatestActivities = [];
    }

    public List<PresenceLocationData> Location { get; set; }
    public List<PresenceActivityData> LatestActivities { get; set; }
}

public sealed class PresenceLocationData
{
    public string LocationId { get; set; }
    public int Probability { get; set; }
    public DateTimeOffset LatestUpdate { get; set; }
}

public class PresenceActivityData
{
    public DateTimeOffset? Date { get; set; }
    public string DeviceId { get; set; }
    public string LocationId { get; set; }
    public string ActivityKind { get; set; }
    public string Status { get; set; }
}

public sealed class PresenceNotificationData : PresenceActivityData
{
    public string AssociatedUser { get; set; }
    public bool IsUserInput { get; set; }
}

public sealed class PresenceUpdateData
{
    public PresenceLocationData Location { get; set; }
    public PresenceActivityData ActivityToLog { get; set; }
}