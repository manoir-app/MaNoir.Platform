using System;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents routine-related data for a user.
/// </summary>
public class RoutineData
{
    public RoutineData()
    {
    }

    public RoutineData(RoutineDataWithUser data)
    {
        NextWakeUpTime = data.NextWakeUpTime;
    }

    /// <summary>
    /// Gets or sets the next planned wake-up time.
    /// </summary>
    public DateTimeOffset? NextWakeUpTime { get; set; }
}

/// <summary>
/// Extends routine data with user identification fields.
/// </summary>
public sealed class RoutineDataWithUser : RoutineData
{
    public RoutineDataWithUser()
    {
    }

    public RoutineDataWithUser(RoutineData data, User user)
    {
        NextWakeUpTime = data.NextWakeUpTime;
        UserId = user.Id;
        UserName = user.Name;
    }

    public RoutineDataWithUser(User user)
    {
        NextWakeUpTime = user.Routine?.NextWakeUpTime;
        UserId = user.Id;
        UserName = user.Name;
    }

    /// <summary>
    /// Gets or sets the related user identifier.
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// Gets or sets the related user display name.
    /// </summary>
    public string UserName { get; set; }
}