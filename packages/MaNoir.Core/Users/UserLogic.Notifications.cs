using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.DataPublication;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

public sealed partial class UserLogic
{
    /// <summary>
    /// Lists a user's notifications ordered by most recent first.
    /// </summary>
    public Task<List<UserNotification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return Task.FromResult<List<UserNotification>>(null);

        return _mongoOperations.GetNotificationsAsync(normalizedUserId, cancellationToken);
    }

    /// <summary>
    /// Stores a notification for a user using the legacy deduplication rules.
    /// </summary>
    public async Task<UserNotification> NotifyUserAsync(string userId, UserNotification notification, bool? sendToMobile = null, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null || notification == null)
            return null;

        PrepareNotificationForSave(normalizedUserId, notification);

        UserNotification savedNotification = await _mongoOperations.SaveNotificationAsync(notification, cancellationToken);
        if (ShouldSendNotificationToMobile(savedNotification, sendToMobile))
        {
            await UserMobileNotificationPublisher.PublishAsync(normalizedUserId, savedNotification, cancellationToken);
        }

        return savedNotification;
    }

    /// <summary>
    /// Marks all notifications older than five seconds as read.
    /// </summary>
    public async Task<bool> MarkAllNotificationsAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return false;

        await _mongoOperations.MarkAllNotificationsAsReadAsync(
            normalizedUserId,
            DateTimeOffset.Now.AddSeconds(-5),
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Marks one notification as read.
    /// </summary>
    public async Task<bool> MarkNotificationAsReadAsync(string userId, string notificationId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        string normalizedNotificationId = NormalizeNotificationId(notificationId);
        if (normalizedUserId == null || normalizedNotificationId == null)
            return false;

        await _mongoOperations.MarkNotificationAsReadAsync(normalizedUserId, normalizedNotificationId, cancellationToken);
        return true;
    }

    /// <summary>
    /// Deletes read notifications older than one minute.
    /// </summary>
    public async Task<bool> ClearReadNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return false;

        DeleteResult result = await _mongoOperations.ClearReadNotificationsAsync(
            normalizedUserId,
            DateTimeOffset.Now.AddMinutes(-1),
            cancellationToken);

        return result.IsAcknowledged;
    }

    internal static string NormalizeNotificationId(string notificationId)
    {
        if (string.IsNullOrWhiteSpace(notificationId))
            return null;

        return notificationId.ToLowerInvariant();
    }

    internal static void PrepareNotificationForSave(string userId, UserNotification notification)
    {
        notification.UserId = userId;
        notification.Id = NormalizeNotificationId(notification.Id);

        if (!string.IsNullOrWhiteSpace(notification.SourceAgent))
        {
            notification.SourceAgent = notification.SourceAgent.ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(notification.Id))
        {
            notification.Id = Guid.NewGuid().ToString().ToLowerInvariant();
        }
    }

    internal static bool ShouldSendNotificationToMobile(UserNotification notification, bool? sendToMobile)
    {
        if (sendToMobile.HasValue)
            return sendToMobile.Value;

        if (notification == null)
            return false;

        switch (notification.Importance)
        {
            case UserNotificationImportance.High:
            case UserNotificationImportance.Critical:
                return true;
            default:
                return false;
        }
    }

}