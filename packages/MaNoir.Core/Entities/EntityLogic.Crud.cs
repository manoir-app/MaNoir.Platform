using MaNoir.Core.Contracts.Models.Entities;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Entities;

public sealed partial class EntityLogic
{
    /// <summary>
    /// Normalizes an entity identifier.
    /// </summary>
    public static string NormalizeEntityId(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
            return null;

        return entityId.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a domain-owned entity kind.
    /// </summary>
    public static string NormalizeEntityKind(string entityKind)
    {
        if (string.IsNullOrWhiteSpace(entityKind))
            return null;

        return entityKind.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes and deduplicates entity kinds.
    /// </summary>
    public static List<string> NormalizeEntityKinds(IEnumerable<string> entityKinds)
    {
        List<string> normalizedKinds = [];
        HashSet<string> seenKinds = new(StringComparer.OrdinalIgnoreCase);

        if (entityKinds == null)
            return normalizedKinds;

        foreach (string entityKind in entityKinds)
        {
            string normalizedKind = NormalizeEntityKind(entityKind);
            if (normalizedKind == null || !seenKinds.Add(normalizedKind))
                continue;

            normalizedKinds.Add(normalizedKind);
        }

        return normalizedKinds;
    }

    /// <summary>
    /// Prepares a native entity for persistence.
    /// </summary>
    public static void PrepareForSave(Entity entity)
    {
        if (entity == null)
            return;

        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString("D").ToLowerInvariant();
        else
            entity.Id = NormalizeEntityId(entity.Id);

        entity.EntityKind = NormalizeEntityKind(entity.EntityKind);
        entity.Source = null;
        entity.LastUpdate = DateTimeOffset.Now;
        entity.Roles ??= [];
        entity.Datas ??= [];
    }

    private static Entity PrepareProjectedEntity(Entity entity, string source, string expectedKind = null, string expectedId = null)
    {
        if (entity == null)
            return null;

        string normalizedKind = NormalizeEntityKind(entity.EntityKind ?? expectedKind);
        string normalizedId = NormalizeEntityId(entity.Id ?? expectedId);
        if (normalizedKind == null || normalizedId == null)
            return null;

        if (expectedKind != null && !string.Equals(normalizedKind, NormalizeEntityKind(expectedKind), StringComparison.OrdinalIgnoreCase))
            return null;

        if (expectedId != null && !string.Equals(normalizedId, NormalizeEntityId(expectedId), StringComparison.OrdinalIgnoreCase))
            return null;

        entity.EntityKind = normalizedKind;
        entity.Id = normalizedId;
        entity.Source = source.Trim();
        entity.Roles ??= [];
        entity.Datas ??= [];
        return entity;
    }
}