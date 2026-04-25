using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Locations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Mesh;

public sealed partial class AutomationMeshLogic
{
    private readonly AutomationMeshMongoOperations _mongoOperations;
    private readonly LocationMongoOperations _locationMongoOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutomationMeshLogic"/> class.
    /// </summary>
    public AutomationMeshLogic()
    {
        _mongoOperations = new AutomationMeshMongoOperations();
        _locationMongoOperations = new LocationMongoOperations();
    }

    /// <summary>
    /// Gets an automation mesh by identifier.
    /// </summary>
    public Task<AutomationMesh> GetByIdAsync(string meshId, CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetByIdAsync(meshId, cancellationToken);
    }

    /// <summary>
    /// Gets the local automation mesh.
    /// </summary>
    public Task<AutomationMesh> GetLocalAsync(CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetLocalAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the local automation mesh, creating or repairing it when needed.
    /// </summary>
    public async Task<AutomationMesh> GetOrCreateLocalAsync(string machineName, string graphApiBaseUri, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        bool hadPublicId = mesh != null && !string.IsNullOrWhiteSpace(mesh.PublicId);
        AutomationMesh ensuredMesh = EnsureLocalMesh(mesh, machineName, graphApiBaseUri);

        if (!ReferenceEquals(mesh, ensuredMesh))
        {
            await SaveAsync(ensuredMesh, cancellationToken);
            return ensuredMesh;
        }

        if (hadPublicId)
            return mesh;

        await SaveAsync(ensuredMesh, cancellationToken);
        return ensuredMesh;
    }

    /// <summary>
    /// Saves an automation mesh aggregate.
    /// </summary>
    public Task SaveAsync(AutomationMesh mesh, CancellationToken cancellationToken = default)
    {
        return _mongoOperations.SaveAsync(mesh, cancellationToken);
    }
}