using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

/// <summary>
/// Provides the first MongoDB-backed operations for user aggregates.
/// </summary>
public sealed class UserMongoOperations
{
    private readonly MongoDbHelper _mongo;
    private readonly IMongoCollection<User> _collection;
    private readonly IMongoCollection<UserNotification> _notificationCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserMongoOperations"/> class.
    /// </summary>
    public UserMongoOperations()
    {
        _mongo = new MongoDbHelper();
        _collection = _mongo.GetCollection<User>();
        _notificationCollection = _mongo.Database.GetCollection<UserNotification>("UserNotifications");
    }

    /// <summary>
    /// Gets the MongoDB collection used for user documents.
    /// </summary>
    public IMongoCollection<User> Collection
    {
        get { return _collection; }
    }

    /// <summary>
    /// Gets the MongoDB collection used for user notification documents.
    /// </summary>
    public IMongoCollection<UserNotification> NotificationCollection
    {
        get { return _notificationCollection; }
    }

    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    public Task<User> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));
        }

        return _collection.Find(user => user.Id == userId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Lists the main household users.
    /// </summary>
    public Task<List<User>> GetMainUsersAsync(CancellationToken cancellationToken = default)
    {
        return _collection.Find(user => user.IsMain).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the current platform administrator when one exists.
    /// </summary>
    public Task<User> GetAdminUserAsync(CancellationToken cancellationToken = default)
    {
        return _collection
            .Find(user => user.IsAdmin)
            .SortBy(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all users.
    /// </summary>
    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _collection.Find(user => true).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all guest users.
    /// </summary>
    public Task<List<User>> GetGuestUsersAsync(CancellationToken cancellationToken = default)
    {
        return _collection.Find(user => user.IsGuest).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all non-guest users.
    /// </summary>
    public Task<List<User>> GetNonGuestUsersAsync(CancellationToken cancellationToken = default)
    {
        return _collection.Find(user => !user.IsGuest).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces a user document by identifier.
    /// </summary>
    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (string.IsNullOrWhiteSpace(user.Id))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(user));
        }

        return _collection.ReplaceOneAsync(
            existingUser => existingUser.Id == user.Id,
            user,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Deletes a user by identifier.
    /// </summary>
    public Task<DeleteResult> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));
        }

        return _collection.DeleteOneAsync(user => user.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Lists all notifications of a user ordered by most recent first.
    /// </summary>
    public Task<List<UserNotification>> GetNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));
        }

        return _notificationCollection
            .Find(notification => notification.UserId == userId)
            .SortByDescending(notification => notification.Date)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Marks every notification older than the supplied threshold as read.
    /// </summary>
    public Task MarkAllNotificationsAsReadAsync(string userId, DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));
        }

        UpdateDefinition<UserNotification> update = Builders<UserNotification>.Update
            .Set(notification => notification.IsRead, true);

        return _notificationCollection.UpdateManyAsync(
            notification => notification.UserId == userId && notification.Date < olderThan,
            update,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Marks a single user notification as read.
    /// </summary>
    public Task MarkNotificationAsReadAsync(string userId, string notificationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(notificationId))
        {
            throw new ArgumentException("The notification identifier cannot be empty.", nameof(notificationId));
        }

        UpdateDefinition<UserNotification> update = Builders<UserNotification>.Update
            .Set(notification => notification.IsRead, true);

        return _notificationCollection.UpdateOneAsync(
            notification => notification.UserId == userId && notification.Id == notificationId,
            update,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes read notifications older than the supplied threshold.
    /// </summary>
    public Task<DeleteResult> ClearReadNotificationsAsync(string userId, DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));
        }

        return _notificationCollection.DeleteManyAsync(
            notification => notification.UserId == userId && notification.IsRead && notification.Date < olderThan,
            cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces a notification using the legacy deduplication rules.
    /// </summary>
    public async Task<UserNotification> SaveNotificationAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification == null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        if (string.IsNullOrWhiteSpace(notification.UserId))
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(notification));
        }

        UserNotification existingNotification = null;
        if (!string.IsNullOrWhiteSpace(notification.SourceAgent) && !string.IsNullOrWhiteSpace(notification.SourceAgentNotificationId))
        {
            existingNotification = await _notificationCollection
                .Find(existing => existing.UserId == notification.UserId
                    && existing.SourceAgent == notification.SourceAgent
                    && existing.SourceAgentNotificationId == notification.SourceAgentNotificationId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(notification.Id))
        {
            existingNotification = await _notificationCollection
                .Find(existing => existing.Id == notification.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (existingNotification == null)
        {
            await _notificationCollection.InsertOneAsync(notification, cancellationToken: cancellationToken);
            return notification;
        }

        notification.Id = existingNotification.Id;
        notification.IsRead = existingNotification.IsRead;

        await _notificationCollection.ReplaceOneAsync(
            existing => existing.Id == notification.Id,
            notification,
            cancellationToken: cancellationToken);

        return notification;
    }
}