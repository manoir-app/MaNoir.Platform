using MaNoir.Core.Contracts.Models.Users;

namespace MaNoir.Core.Users;

public sealed partial class UserLogic
{
    /// <summary>
    /// Removes sensitive or non-essential data from a user projection.
    /// </summary>
    /// <param name="user">The user projection to trim.</param>
    public static void MinimizeData(User user)
    {
        if (user == null)
            return;

        user.HealthData = null;
        user.HashedPinCode = null;
        user.HashedPassword = null;
        user.Presence = null;
    }

    /// <summary>
    /// Prepares a user projection for presence-oriented use cases.
    /// </summary>
    /// <param name="user">The user projection to trim.</param>
    public static void PrepareForPresence(User user)
    {
        if (user == null)
            return;

        user.HealthData = null;
        user.HashedPinCode = null;
        user.HashedPassword = null;
    }
}