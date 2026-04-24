using System;

namespace MaNoir.Core.Contracts.Models.Users;

public class RoutineData
{
    public RoutineData()
    {
    }

    public RoutineData(RoutineDataWithUser data)
    {
        NextWakeUpTime = data.NextWakeUpTime;
    }

    public DateTimeOffset? NextWakeUpTime { get; set; }
}

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

    public string UserId { get; set; }
    public string UserName { get; set; }
}