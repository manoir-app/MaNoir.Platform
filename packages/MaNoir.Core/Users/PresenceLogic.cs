using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Mesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

/// <summary>
/// Implements presence computations and persistence for users.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// PresenceLogic logic = new PresenceLogic();
/// PresenceChangeSet changeSet = await logic.HandleActivityAsync(new PresenceNotificationData()
/// {
///     AssociatedUser = "michael",
///     ActivityKind = "personaldevicedetection",
///     LocationId = "home",
///     Status = "in"
/// }, cancellationToken: cancellationToken);
/// </code>
/// <para>
/// This logic updates user presence probabilities and keeps mesh privacy mode aligned with guest presence.
/// </para>
/// </remarks>
public sealed class PresenceLogic
{
    private const int PresentProbabilityThreshold = 50;

    private readonly AutomationMeshLogic _automationMeshLogic;
    private readonly UserLogic _userLogic;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresenceLogic"/> class.
    /// </summary>
    public PresenceLogic()
    {
        _automationMeshLogic = new AutomationMeshLogic();
        _userLogic = new UserLogic();
    }

    /// <summary>
    /// Applies one presence notification to the targeted user and returns the resulting transition set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned <see cref="PresenceChangeSet"/> makes it easy for callers such as Erza to publish events only when the present/absent state actually changed.
    /// </para>
    /// </remarks>
    public async Task<PresenceChangeSet> HandleActivityAsync(PresenceNotificationData notification, string localLocationId = null, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = UserLogic.NormalizeUserId(notification?.AssociatedUser);
        if (normalizedUserId == null)
            return null;

        User user = await _userLogic.GetByIdAsync(normalizedUserId, cancellationToken);
        if (user == null)
            return null;

        EnsurePresenceInitialized(user);
        bool wasPresent = IsUserPresent(user);
        DecayPresence(user, DateTimeOffset.UtcNow);
        PruneOldActivities(user, DateTimeOffset.UtcNow.AddHours(-2));

        PresenceNotificationData preparedNotification = PrepareNotification(notification);
        PresenceUpdateData update = new PresenceUpdateData();

        PresenceActivityData activity = new PresenceActivityData()
        {
            ActivityKind = preparedNotification.ActivityKind,
            Date = preparedNotification.Date,
            DeviceId = preparedNotification.DeviceId,
            LocationId = preparedNotification.LocationId,
            Status = preparedNotification.Status
        };

        if (ShouldTrace(user, activity))
            update.ActivityToLog = activity;

        PresenceLocationData location = FindLocation(user, preparedNotification, localLocationId);
        if (location != null)
            update.Location = location;

        PresenceChangeSet changeSet = new PresenceChangeSet()
        {
            UpdatedUser = user
        };

        if (update.ActivityToLog == null && update.Location == null)
            return changeSet;

        MergePresenceUpdate(user, update);
        await _userLogic.SaveAsync(user, cancellationToken);

        bool isPresent = IsUserPresent(user);
        AppendTransition(changeSet, user.Id, wasPresent, isPresent);
        await RefreshMeshPrivacyModeAsync(cancellationToken);
        return changeSet;
    }

    /// <summary>
    /// Gets users whose current location probability is high enough to consider them present.
    /// </summary>
    /// <remarks>
    /// <para>This method uses the current in-memory probability threshold and does not mutate stored presence.</para>
    /// </remarks>
    public async Task<List<User>> GetPresentUsersAsync(CancellationToken cancellationToken = default)
    {
        List<User> users = await _userLogic.GetAllAsync(cancellationToken);
        List<User> presentUsers = [];

        foreach (User user in users)
        {
            if (user?.Presence?.Location == null)
                continue;

            if (user.Presence.Location.Any(location => location != null && location.Probability >= PresentProbabilityThreshold))
                presentUsers.Add(user);
        }

        return presentUsers;
    }

    /// <summary>
    /// Decays stored presence probabilities and reports the users that changed presence state.
    /// </summary>
    /// <remarks>
    /// <para>Run this periodically from a background worker to let stale presence fade out over time.</para>
    /// </remarks>
    public async Task<PresenceChangeSet> RunMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        HashSet<string> previouslyPresentUserIds = await GetPresentUserIdSetAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<User> users = await _userLogic.GetAllAsync(cancellationToken);

        foreach (User user in users)
        {
            if (user == null)
                continue;

            EnsurePresenceInitialized(user);
            if (!DecayPresence(user, now))
                continue;

            await _userLogic.SaveAsync(user, cancellationToken);
        }

        await RefreshMeshPrivacyModeAsync(cancellationToken);

        HashSet<string> currentlyPresentUserIds = await GetPresentUserIdSetAsync(cancellationToken);
        PresenceChangeSet changeSet = new PresenceChangeSet();

        foreach (string userId in currentlyPresentUserIds)
        {
            if (!previouslyPresentUserIds.Contains(userId))
                changeSet.NewlyPresentUserIds.Add(userId);
        }

        foreach (string userId in previouslyPresentUserIds)
        {
            if (!currentlyPresentUserIds.Contains(userId))
                changeSet.NewlyAbsentUserIds.Add(userId);
        }

        changeSet.NewlyPresentUserIds.Sort(StringComparer.OrdinalIgnoreCase);
        changeSet.NewlyAbsentUserIds.Sort(StringComparer.OrdinalIgnoreCase);
        return changeSet;
    }

    /// <summary>
    /// Recomputes mesh privacy mode from the currently present users.
    /// </summary>
    /// <remarks>
    /// <para>Guest presence enables high privacy mode, while a mesh with only main users clears it.</para>
    /// </remarks>
    public Task RefreshMeshPrivacyModeAsync(CancellationToken cancellationToken = default)
    {
        return RefreshMeshPrivacyModeCoreAsync(cancellationToken);
    }

    private async Task RefreshMeshPrivacyModeCoreAsync(CancellationToken cancellationToken)
    {
        List<User> presentUsers = await GetPresentUsersAsync(cancellationToken);
        bool hasGuestPresent = presentUsers.Any(user => user != null && !user.IsMain);

        if (hasGuestPresent)
            await _automationMeshLogic.SetPrivacyModeAsync(AutomationMeshPrivacyMode.High, cancellationToken);
        else
            await _automationMeshLogic.ClearPrivacyModeAsync(cancellationToken);
    }

    private static PresenceNotificationData PrepareNotification(PresenceNotificationData notification)
    {
        PresenceNotificationData preparedNotification = new PresenceNotificationData()
        {
            ActivityKind = string.IsNullOrWhiteSpace(notification?.ActivityKind) ? "notification" : notification.ActivityKind.Trim().ToLowerInvariant(),
            AssociatedUser = UserLogic.NormalizeUserId(notification?.AssociatedUser),
            Date = notification?.Date ?? DateTimeOffset.UtcNow,
            DeviceId = NormalizeText(notification?.DeviceId),
            IsUserInput = notification?.IsUserInput == true,
            LocationId = NormalizeText(notification?.LocationId),
            Status = string.IsNullOrWhiteSpace(notification?.Status) ? null : notification.Status.Trim().ToLowerInvariant()
        };

        return preparedNotification;
    }

    private static bool ShouldTrace(User user, PresenceActivityData activity)
    {
        IEnumerable<PresenceActivityData> latestActivities = user?.Presence?.LatestActivities ?? [];
        bool hasAlready = latestActivities.Any(existingActivity =>
            string.Equals(existingActivity.ActivityKind, activity.ActivityKind, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existingActivity.LocationId, activity.LocationId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existingActivity.Status, activity.Status, StringComparison.OrdinalIgnoreCase));

        return !hasAlready;
    }

    private static PresenceLocationData FindLocation(User user, PresenceNotificationData notification, string localLocationId)
    {
        switch (notification.ActivityKind)
        {
            case "mobileappusage":
                return FindLocationForMobileAppUsage(user, notification, localLocationId);
            case "forcelocation":
                return BuildBinaryLocation(notification.LocationId, notification.Status);
            case "fromevent":
                return BuildEventLocation(notification.LocationId, notification.Status);
            case "personaldevicedetection":
                return BuildPersonalDeviceDetectionLocation(user, notification);
            default:
                return null;
        }
    }

    private static PresenceLocationData FindLocationForMobileAppUsage(User user, PresenceNotificationData notification, string localLocationId)
    {
        string normalizedStatus = string.IsNullOrWhiteSpace(notification.Status) ? "info" : notification.Status;
        if (!string.Equals(normalizedStatus, "homeautomation", StringComparison.OrdinalIgnoreCase))
            return null;

        string locationId = NormalizeText(notification.LocationId) ?? NormalizeText(localLocationId);
        if (locationId == null)
            return null;

        return new PresenceLocationData()
        {
            LocationId = locationId,
            Probability = CalculateProbability(user?.Presence?.Location, locationId, 10),
            LatestUpdate = DateTimeOffset.UtcNow
        };
    }

    private static PresenceLocationData BuildBinaryLocation(string locationId, string status)
    {
        string normalizedLocationId = NormalizeText(locationId);
        if (normalizedLocationId == null)
            return null;

        if (string.Equals(status, "in", StringComparison.OrdinalIgnoreCase))
            return new PresenceLocationData() { LocationId = normalizedLocationId, Probability = 100, LatestUpdate = DateTimeOffset.UtcNow };

        if (string.Equals(status, "out", StringComparison.OrdinalIgnoreCase))
            return new PresenceLocationData() { LocationId = normalizedLocationId, Probability = 0, LatestUpdate = DateTimeOffset.UtcNow };

        return null;
    }

    private static PresenceLocationData BuildEventLocation(string locationId, string status)
    {
        string normalizedLocationId = NormalizeText(locationId);
        if (normalizedLocationId == null)
            return null;

        if (string.Equals(status, "start", StringComparison.OrdinalIgnoreCase))
            return new PresenceLocationData() { LocationId = normalizedLocationId, Probability = 100, LatestUpdate = DateTimeOffset.UtcNow };

        if (string.Equals(status, "end", StringComparison.OrdinalIgnoreCase))
            return new PresenceLocationData() { LocationId = normalizedLocationId, Probability = 0, LatestUpdate = DateTimeOffset.UtcNow };

        return null;
    }

    private static PresenceLocationData BuildPersonalDeviceDetectionLocation(User user, PresenceNotificationData notification)
    {
        string normalizedLocationId = NormalizeText(notification.LocationId);
        if (normalizedLocationId == null)
            return null;

        if (string.Equals(notification.Status, "in", StringComparison.OrdinalIgnoreCase))
        {
            return new PresenceLocationData()
            {
                LocationId = normalizedLocationId,
                Probability = CalculateProbability(user?.Presence?.Location, normalizedLocationId, 60),
                LatestUpdate = DateTimeOffset.UtcNow
            };
        }

        if (string.Equals(notification.Status, "out", StringComparison.OrdinalIgnoreCase))
        {
            return new PresenceLocationData()
            {
                LocationId = normalizedLocationId,
                Probability = CalculateProbability(user?.Presence?.Location, normalizedLocationId, -75),
                LatestUpdate = DateTimeOffset.UtcNow
            };
        }

        return null;
    }

    private static int CalculateProbability(List<PresenceLocationData> locations, string locationId, int delta)
    {
        PresenceLocationData existingLocation = locations?.FirstOrDefault(location => string.Equals(location?.LocationId, locationId, StringComparison.OrdinalIgnoreCase));
        if (existingLocation != null)
            return Math.Min(100, Math.Max(0, existingLocation.Probability + delta));

        return Math.Max(5, Math.Min(100, delta));
    }

    private static void MergePresenceUpdate(User user, PresenceUpdateData update)
    {
        if (update?.Location != null)
        {
            PresenceLocationData existingLocation = user.Presence.Location.FirstOrDefault(location => string.Equals(location.LocationId, update.Location.LocationId, StringComparison.OrdinalIgnoreCase));
            if (existingLocation == null)
                user.Presence.Location.Add(update.Location);
            else
            {
                existingLocation.Probability = update.Location.Probability;
                existingLocation.LatestUpdate = update.Location.LatestUpdate;
            }
        }

        if (update?.ActivityToLog != null)
            user.Presence.LatestActivities.Add(update.ActivityToLog);
    }

    private static bool DecayPresence(User user, DateTimeOffset now)
    {
        bool changed = false;
        foreach (PresenceLocationData location in user?.Presence?.Location ?? [])
        {
            if (location == null)
                continue;

            if (location.LatestUpdate < now.AddHours(-12))
            {
                if (location.Probability != 0)
                {
                    location.Probability = 0;
                    location.LatestUpdate = now;
                    changed = true;
                }

                continue;
            }

            if (location.LatestUpdate < now.AddMinutes(-15))
            {
                int newProbability = Math.Max(5, location.Probability - 5);
                if (newProbability != location.Probability)
                {
                    location.Probability = newProbability;
                    location.LatestUpdate = now;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static void PruneOldActivities(User user, DateTimeOffset threshold)
    {
        user.Presence.LatestActivities = user.Presence.LatestActivities
            .Where(activity => activity?.Date.GetValueOrDefault() > threshold)
            .ToList();
    }

    private static void EnsurePresenceInitialized(User user)
    {
        user.Presence ??= new PresenceData();
        user.Presence.Location ??= [];
        user.Presence.LatestActivities ??= [];
    }

    private async Task<HashSet<string>> GetPresentUserIdSetAsync(CancellationToken cancellationToken)
    {
        List<User> presentUsers = await GetPresentUsersAsync(cancellationToken);
        return new HashSet<string>(presentUsers.Where(user => user != null && user.Id != null).Select(user => user.Id), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsUserPresent(User user)
    {
        return user?.Presence?.Location?.Any(location => location != null && location.Probability >= PresentProbabilityThreshold) == true;
    }

    private static void AppendTransition(PresenceChangeSet changeSet, string userId, bool wasPresent, bool isPresent)
    {
        if (changeSet == null || string.IsNullOrWhiteSpace(userId) || wasPresent == isPresent)
            return;

        if (isPresent)
            changeSet.NewlyPresentUserIds.Add(userId);
        else
            changeSet.NewlyAbsentUserIds.Add(userId);
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}