using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Mongo;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="UserMongoOperations"/> class.
    /// </summary>
    public UserMongoOperations()
    {
        _mongo = new MongoDbHelper();
        _collection = _mongo.GetCollection<User>();
    }

    /// <summary>
    /// Gets the MongoDB collection used for user documents.
    /// </summary>
    public IMongoCollection<User> Collection
    {
        get { return _collection; }
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
}