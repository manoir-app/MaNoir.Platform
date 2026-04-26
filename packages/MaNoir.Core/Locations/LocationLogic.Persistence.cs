using MaNoir.Core.Contracts.Models.Locations;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Locations;

public sealed partial class LocationLogic
{
    /// <summary>
    /// Gets all locations.
    /// </summary>
    public Task<List<Location>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a location by identifier.
    /// </summary>
    public Task<Location> GetByIdAsync(string locationId, CancellationToken cancellationToken = default)
    {
        string normalizedLocationId = NormalizeLocationId(locationId);
        if (normalizedLocationId == null)
            return Task.FromResult<Location>(null);

        return _mongoOperations.GetByIdAsync(normalizedLocationId, cancellationToken);
    }

    /// <summary>
    /// Creates or updates a location and persists it.
    /// </summary>
    public async Task<Location> UpsertAsync(Location location, CancellationToken cancellationToken = default)
    {
        if (location == null)
            return null;

        PrepareForSave(location);
        await _mongoOperations.SaveAsync(location, cancellationToken);
        return await GetByIdAsync(location.Id, cancellationToken);
    }

    /// <summary>
    /// Deletes a location by identifier.
    /// </summary>
    public async Task<bool> DeleteAsync(string locationId, CancellationToken cancellationToken = default)
    {
        string normalizedLocationId = NormalizeLocationId(locationId);
        if (normalizedLocationId == null)
            return false;

        DeleteResult deleteResult = await _mongoOperations.DeleteAsync(normalizedLocationId, cancellationToken);
        return deleteResult.DeletedCount == 1;
    }
}