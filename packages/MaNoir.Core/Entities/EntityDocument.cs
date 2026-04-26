using MaNoir.Core.Contracts.Models.Entities;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Entities;

internal sealed class EntityDocument
{
    [BsonId]
    public string Key { get; set; }

    public string Id { get; set; }

    public string EntityKind { get; set; }

    public string Name { get; set; }

    public string DefaultImageUrl { get; set; }

    public string CurrentImageUrl { get; set; }

    public string MeshId { get; set; }

    public string LocationId { get; set; }

    public DateTimeOffset LastUpdate { get; set; }

    public List<string> Roles { get; set; }

    public Dictionary<string, EntityData> Datas { get; set; }

    public static string ComposeKey(string entityKind, string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityKind))
            throw new ArgumentException("The entity kind cannot be empty.", nameof(entityKind));

        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("The entity identifier cannot be empty.", nameof(entityId));

        return string.Concat(entityKind, "::", entityId);
    }

    public static EntityDocument FromEntity(Entity entity)
    {
        return new EntityDocument()
        {
            Key = ComposeKey(entity.EntityKind, entity.Id),
            Id = entity.Id,
            EntityKind = entity.EntityKind,
            Name = entity.Name,
            DefaultImageUrl = entity.DefaultImageUrl,
            CurrentImageUrl = entity.CurrentImageUrl,
            MeshId = entity.MeshId,
            LocationId = entity.LocationId,
            LastUpdate = entity.LastUpdate,
            Roles = entity.Roles == null ? [] : [.. entity.Roles],
            Datas = entity.Datas == null ? [] : new Dictionary<string, EntityData>(entity.Datas)
        };
    }

    public Entity ToEntity()
    {
        return new Entity()
        {
            Id = Id,
            EntityKind = EntityKind,
            Name = Name,
            DefaultImageUrl = DefaultImageUrl,
            CurrentImageUrl = CurrentImageUrl,
            MeshId = MeshId,
            LocationId = LocationId,
            LastUpdate = LastUpdate,
            Roles = Roles == null ? [] : [.. Roles],
            Datas = Datas == null ? [] : new Dictionary<string, EntityData>(Datas)
        };
    }
}