using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Entities;

/// <summary>
/// Provides MongoDB-backed operations for native entities.
/// </summary>
public sealed class EntityMongoOperations
{
    private readonly IMongoCollection<EntityDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityMongoOperations"/> class.
    /// </summary>
    public EntityMongoOperations()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _collection = mongo.GetCollection<EntityDocument>();
    }

    /// <summary>
    /// Gets a native entity by kind and identifier.
    /// </summary>
    public async Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("The entity kind cannot be empty.", nameof(kind));

        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("The entity identifier cannot be empty.", nameof(entityId));

        EntityDocument document = await _collection
            .Find(entity => entity.Key == EntityDocument.ComposeKey(kind, entityId))
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToEntity();
    }

    /// <summary>
    /// Gets native entities for a set of kinds.
    /// </summary>
    public async Task<List<Entity>> GetByKindsAsync(IReadOnlyCollection<string> kinds, CancellationToken cancellationToken = default)
    {
        if (kinds == null || kinds.Count == 0)
            throw new ArgumentException("At least one entity kind must be provided.", nameof(kinds));

        List<EntityDocument> documents = await _collection
            .Find(entity => kinds.Contains(entity.EntityKind))
            .ToListAsync(cancellationToken);

        List<Entity> entities = [];
        foreach (EntityDocument document in documents)
            entities.Add(document.ToEntity());

        return entities;
    }

    /// <summary>
    /// Inserts or replaces a native entity by kind and identifier.
    /// </summary>
    public Task SaveAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.EntityKind))
            throw new ArgumentException("The entity kind cannot be empty.", nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new ArgumentException("The entity identifier cannot be empty.", nameof(entity));

        EntityDocument document = EntityDocument.FromEntity(entity);
        return _collection.ReplaceOneAsync(
            existingEntity => existingEntity.Key == document.Key,
            document,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Deletes a native entity by kind and identifier.
    /// </summary>
    public Task<DeleteResult> DeleteAsync(string kind, string entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("The entity kind cannot be empty.", nameof(kind));

        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("The entity identifier cannot be empty.", nameof(entityId));

        return _collection.DeleteOneAsync(entity => entity.Key == EntityDocument.ComposeKey(kind, entityId), cancellationToken);
    }
}