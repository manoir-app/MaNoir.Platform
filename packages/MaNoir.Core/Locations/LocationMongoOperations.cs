using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Locations;

/// <summary>
/// Provides MongoDB-backed operations for locations.
/// </summary>
public sealed class LocationMongoOperations
{
    private readonly IMongoCollection<Location> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationMongoOperations"/> class.
    /// </summary>
    public LocationMongoOperations()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _collection = mongo.GetCollection<Location>();
    }

    /// <summary>
    /// Gets all locations.
    /// </summary>
    public Task<List<Location>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _collection.Find(location => true).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a location by identifier.
    /// </summary>
    public Task<Location> GetByIdAsync(string locationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(locationId))
            throw new ArgumentException("The location identifier cannot be empty.", nameof(locationId));

        return _collection.Find(location => location.Id == locationId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces a location by identifier.
    /// </summary>
    public Task SaveAsync(Location location, CancellationToken cancellationToken = default)
    {
        if (location == null)
            throw new ArgumentNullException(nameof(location));

        if (string.IsNullOrWhiteSpace(location.Id))
            throw new ArgumentException("The location identifier cannot be empty.", nameof(location));

        return _collection.ReplaceOneAsync(
            existingLocation => existingLocation.Id == location.Id,
            location,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Deletes a location by identifier.
    /// </summary>
    public Task<DeleteResult> DeleteAsync(string locationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(locationId))
            throw new ArgumentException("The location identifier cannot be empty.", nameof(locationId));

        return _collection.DeleteOneAsync(location => location.Id == locationId, cancellationToken);
    }
}