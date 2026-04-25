using MaNoir.Core.Contracts.Models.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Entities;

public sealed partial class EntityLogic
{
    /// <summary>
    /// Gets an entity by kind and identifier.
    /// </summary>
    public async Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default)
    {
        string normalizedKind = NormalizeEntityKind(kind);
        string normalizedEntityId = NormalizeEntityId(entityId);
        if (normalizedKind == null || normalizedEntityId == null)
            return null;

        Entity nativeEntity = await _mongoOperations.GetByIdAsync(normalizedKind, normalizedEntityId, cancellationToken);
        if (nativeEntity != null)
            return nativeEntity;

        foreach (IProjectedEntityRepository repository in _projectionRepositoryRegistry.GetRepositoriesForKinds([normalizedKind]))
        {
            Entity projectedEntity = await repository.GetByIdAsync(normalizedKind, normalizedEntityId, cancellationToken);
            projectedEntity = PrepareProjectedEntity(projectedEntity, repository.Source, normalizedKind, normalizedEntityId);
            if (projectedEntity != null)
                return projectedEntity;
        }

        return null;
    }

    /// <summary>
    /// Gets all entities for one kind.
    /// </summary>
    public Task<List<Entity>> GetByKindAsync(string kind, CancellationToken cancellationToken = default)
    {
        return GetByKindsAsync([kind], cancellationToken);
    }

    /// <summary>
    /// Gets entities for a set of kinds.
    /// </summary>
    public async Task<List<Entity>> GetByKindsAsync(IEnumerable<string> kinds, CancellationToken cancellationToken = default)
    {
        List<string> normalizedKinds = NormalizeEntityKinds(kinds);
        if (normalizedKinds.Count == 0)
            return [];

        Dictionary<string, Entity> entitiesByKey = new(StringComparer.OrdinalIgnoreCase);

        List<Entity> nativeEntities = await _mongoOperations.GetByKindsAsync(normalizedKinds, cancellationToken);
        foreach (Entity entity in nativeEntities)
        {
            if (entity == null)
                continue;

            string normalizedKind = NormalizeEntityKind(entity.EntityKind);
            string normalizedId = NormalizeEntityId(entity.Id);
            if (normalizedKind == null || normalizedId == null)
                continue;

            entity.EntityKind = normalizedKind;
            entity.Id = normalizedId;
            entitiesByKey[EntityDocument.ComposeKey(normalizedKind, normalizedId)] = entity;
        }

        foreach (IProjectedEntityRepository repository in _projectionRepositoryRegistry.GetRepositoriesForKinds(normalizedKinds))
        {
            List<Entity> projectedEntities = await repository.GetByKindsAsync(normalizedKinds, cancellationToken);
            if (projectedEntities == null)
                continue;

            foreach (Entity entity in projectedEntities)
            {
                Entity projectedEntity = PrepareProjectedEntity(entity, repository.Source);
                if (projectedEntity == null)
                    continue;

                if (!normalizedKinds.Contains(projectedEntity.EntityKind))
                    continue;

                string key = EntityDocument.ComposeKey(projectedEntity.EntityKind, projectedEntity.Id);
                entitiesByKey.TryAdd(key, projectedEntity);
            }
        }

        return [.. entitiesByKey.Values];
    }

    /// <summary>
    /// Creates or updates a native entity and persists it.
    /// </summary>
    public async Task<Entity> UpsertAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null || entity.IsReadOnly)
            return null;

        PrepareForSave(entity);
        if (entity.Id == null || entity.EntityKind == null)
            return null;

        await _mongoOperations.SaveAsync(entity, cancellationToken);
        return await GetByIdAsync(entity.EntityKind, entity.Id, cancellationToken);
    }

    /// <summary>
    /// Deletes a native entity by kind and identifier.
    /// </summary>
    public async Task<bool> DeleteAsync(string kind, string entityId, CancellationToken cancellationToken = default)
    {
        string normalizedKind = NormalizeEntityKind(kind);
        string normalizedEntityId = NormalizeEntityId(entityId);
        if (normalizedKind == null || normalizedEntityId == null)
            return false;

        DeleteResult deleteResult = await _mongoOperations.DeleteAsync(normalizedKind, normalizedEntityId, cancellationToken);
        return deleteResult.DeletedCount == 1;
    }
}