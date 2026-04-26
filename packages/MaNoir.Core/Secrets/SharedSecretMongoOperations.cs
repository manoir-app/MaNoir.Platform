using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Secrets;

/// <summary>
/// Provides MongoDB-backed operations for shared secrets.
/// </summary>
public sealed class SharedSecretMongoOperations
{
    private readonly IMongoCollection<SharedSecret> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedSecretMongoOperations"/> class.
    /// </summary>
    public SharedSecretMongoOperations()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _collection = mongo.GetCollection<SharedSecret>();
    }

    /// <summary>
    /// Gets one shared secret by identifier.
    /// </summary>
    public Task<SharedSecret> GetByIdAsync(string secretId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretId))
            throw new ArgumentException("The shared secret identifier cannot be empty.", nameof(secretId));

        return _collection.Find(secret => secret.Id == secretId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces one shared secret by identifier.
    /// </summary>
    public Task SaveAsync(SharedSecret secret, CancellationToken cancellationToken = default)
    {
        if (secret == null)
            throw new ArgumentNullException(nameof(secret));

        if (string.IsNullOrWhiteSpace(secret.Id))
            throw new ArgumentException("The shared secret identifier cannot be empty.", nameof(secret));

        return _collection.ReplaceOneAsync(
            existingSecret => existingSecret.Id == secret.Id,
            secret,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Deletes one shared secret by identifier.
    /// </summary>
    public Task<DeleteResult> DeleteAsync(string secretId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretId))
            throw new ArgumentException("The shared secret identifier cannot be empty.", nameof(secretId));

        return _collection.DeleteOneAsync(secret => secret.Id == secretId, cancellationToken);
    }
}