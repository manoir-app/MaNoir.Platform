using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Authorization;

internal sealed class AuthorizationMongoOperations
{
    private readonly IMongoCollection<AccessZoneDefinition> _accessZoneCollection;
    private readonly IMongoCollection<UserAccessGrantDocument> _grantCollection;

    public AuthorizationMongoOperations()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _accessZoneCollection = mongo.Database.GetCollection<AccessZoneDefinition>("AccessZoneDefinitions");
        _grantCollection = mongo.Database.GetCollection<UserAccessGrantDocument>("UserAccessGrants");
    }

    public Task<List<AccessZoneDefinition>> GetAccessZoneDefinitionsAsync(string pluginId = null, CancellationToken cancellationToken = default)
    {
        FilterDefinitionBuilder<AccessZoneDefinition> filterBuilder = Builders<AccessZoneDefinition>.Filter;
        FilterDefinition<AccessZoneDefinition> filter = FilterDefinition<AccessZoneDefinition>.Empty;

        if (!string.IsNullOrWhiteSpace(pluginId))
            filter &= filterBuilder.Eq(definition => definition.PluginId, pluginId);

        return _accessZoneCollection
            .Find(filter)
            .SortBy(definition => definition.Id)
            .ToListAsync(cancellationToken);
    }

    public Task SaveAccessZoneDefinitionAsync(AccessZoneDefinition definition, CancellationToken cancellationToken = default)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("The access zone identifier cannot be empty.", nameof(definition));

        return _accessZoneCollection.ReplaceOneAsync(
            existingDefinition => existingDefinition.Id == definition.Id,
            definition,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }

    public Task DeleteAccessZoneDefinitionAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            throw new ArgumentException("The access zone identifier cannot be empty.", nameof(zoneId));

        return _accessZoneCollection.DeleteOneAsync(definition => definition.Id == zoneId, cancellationToken);
    }

    public Task<List<UserAccessGrantDocument>> GetUserAccessGrantsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));

        return _grantCollection
            .Find(grant => grant.UserId == userId)
            .SortBy(grant => grant.ZoneId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceUserAccessGrantsAsync(string userId, IReadOnlyCollection<UserAccessGrantDocument> grants, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));

        await _grantCollection.DeleteManyAsync(grant => grant.UserId == userId, cancellationToken);

        if (grants == null || grants.Count == 0)
            return;

        await _grantCollection.InsertManyAsync(grants, cancellationToken: cancellationToken);
    }
}