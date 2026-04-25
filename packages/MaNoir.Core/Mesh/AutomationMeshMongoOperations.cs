using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Mongo;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Mesh;

/// <summary>
/// Provides the first MongoDB-backed operations for automation mesh aggregates.
/// </summary>
public sealed class AutomationMeshMongoOperations
{
    private readonly MongoDbHelper _mongo;
    private readonly IMongoCollection<AutomationMesh> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutomationMeshMongoOperations"/> class.
    /// </summary>
    public AutomationMeshMongoOperations()
    {
        _mongo = new MongoDbHelper();
        _collection = _mongo.GetCollection<AutomationMesh>();
    }

    /// <summary>
    /// Gets the MongoDB collection used for automation mesh documents.
    /// </summary>
    public IMongoCollection<AutomationMesh> Collection
    {
        get { return _collection; }
    }

    /// <summary>
    /// Gets an automation mesh by identifier.
    /// </summary>
    public Task<AutomationMesh> GetByIdAsync(string meshId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(meshId))
        {
            throw new ArgumentException("The mesh identifier cannot be empty.", nameof(meshId));
        }

        return _collection.Find(mesh => mesh.Id == meshId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the local automation mesh.
    /// </summary>
    public Task<AutomationMesh> GetLocalAsync(CancellationToken cancellationToken = default)
    {
        return GetByIdAsync("local", cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces an automation mesh document by identifier.
    /// </summary>
    public Task SaveAsync(AutomationMesh mesh, CancellationToken cancellationToken = default)
    {
        if (mesh == null)
        {
            throw new ArgumentNullException(nameof(mesh));
        }

        if (string.IsNullOrWhiteSpace(mesh.Id))
        {
            throw new ArgumentException("The mesh identifier cannot be empty.", nameof(mesh));
        }

        return _collection.ReplaceOneAsync(
            existingMesh => existingMesh.Id == mesh.Id,
            mesh,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }
}