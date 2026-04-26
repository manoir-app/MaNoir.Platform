using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Entities;

/// <summary>
/// Represents a generic entity that can be stored natively or projected from another model.
/// </summary>
public sealed class Entity
{
    public Entity()
    {
        Roles = [];
        Datas = [];
        LastUpdate = DateTimeOffset.Now;
    }

    /// <summary>
    /// Gets or sets the entity identifier inside its kind.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the domain-owned entity kind.
    /// </summary>
    public string EntityKind { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the default image URL.
    /// </summary>
    public string DefaultImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the current image URL.
    /// </summary>
    public string CurrentImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the owning mesh identifier when applicable.
    /// </summary>
    public string MeshId { get; set; }

    /// <summary>
    /// Gets or sets the related location identifier when applicable.
    /// </summary>
    public string LocationId { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset LastUpdate { get; set; }

    /// <summary>
    /// Gets or sets the entity roles.
    /// </summary>
    public List<string> Roles { get; set; }

    /// <summary>
    /// Gets or sets the entity data points.
    /// </summary>
    public Dictionary<string, EntityData> Datas { get; set; }

    /// <summary>
    /// Gets or sets the projection source when the entity is read-only.
    /// </summary>
    [BsonIgnore]
    public string Source { get; set; }

    /// <summary>
    /// Gets a value indicating whether the entity comes from a projected source.
    /// </summary>
    [BsonIgnore]
    public bool IsReadOnly => !string.IsNullOrWhiteSpace(Source);
}

/// <summary>
/// Represents a generic entity data point.
/// </summary>
public sealed class EntityData
{
    public EntityData()
    {
        ComplexValue = [];
        Category = string.Empty;
    }

    /// <summary>
    /// Gets or sets the CLR simple type name.
    /// </summary>
    public string SimpleType { get; set; }

    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    public string SimpleValue { get; set; }

    /// <summary>
    /// Gets or sets the integer value.
    /// </summary>
    public long? IntSimpleValue { get; set; }

    /// <summary>
    /// Gets or sets the decimal value.
    /// </summary>
    public decimal? DecimalSimpleValue { get; set; }

    /// <summary>
    /// Gets or sets the date value.
    /// </summary>
    public DateTimeOffset? DateSimpleValue { get; set; }

    /// <summary>
    /// Gets or sets the complex nested value.
    /// </summary>
    public Dictionary<string, EntityData> ComplexValue { get; set; }

    /// <summary>
    /// Gets or sets the domain-owned category.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Gets or sets the domain-owned data class.
    /// </summary>
    public string DataClass { get; set; }
}