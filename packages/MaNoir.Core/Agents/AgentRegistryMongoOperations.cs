using MaNoir.Core.Contracts.Models.Agents;
using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Agents;

internal sealed class AgentRegistryMongoOperations
{
    private readonly IMongoCollection<RegisteredAgent> _collection;

    public AgentRegistryMongoOperations()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _collection = mongo.Database.GetCollection<RegisteredAgent>("Agents");
    }

    public Task<List<RegisteredAgent>> GetAgentsAsync(string meshId = null, CancellationToken cancellationToken = default)
    {
        FilterDefinition<RegisteredAgent> filter = FilterDefinition<RegisteredAgent>.Empty;
        if (!string.IsNullOrWhiteSpace(meshId))
            filter &= Builders<RegisteredAgent>.Filter.Eq(agent => agent.MeshId, meshId);

        return _collection
            .Find(filter)
            .SortBy(agent => agent.MeshId)
            .ThenBy(agent => agent.AgentId)
            .ToListAsync(cancellationToken);
    }

    public Task<RegisteredAgent> GetAgentAsync(string meshId, string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(meshId))
            throw new ArgumentException("The mesh identifier cannot be empty.", nameof(meshId));

        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentException("The agent identifier cannot be empty.", nameof(agentId));

        return _collection
            .Find(agent => agent.MeshId == meshId && agent.AgentId == agentId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveAgentAsync(RegisteredAgent agent, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        return _collection.ReplaceOneAsync(existingAgent => existingAgent.Id == agent.Id, agent, new ReplaceOptions() { IsUpsert = true }, cancellationToken);
    }
}