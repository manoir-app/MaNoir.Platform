using System;
using System.Collections.Generic;
using System.Linq;
using MaNoir.Core.Locations;
using MaNoir.Core.Mesh;
using MaNoir.Core.Users;

namespace MaNoir.Core.Entities;

/// <summary>
/// Registers read-only projected entity repositories.
/// </summary>
public sealed class EntityProjectionRepositoryRegistry
{
    private readonly object _syncRoot = new();
    private readonly List<IProjectedEntityRepository> _repositories = [];

    /// <summary>
    /// Creates the default registry populated with built-in Core projections.
    /// </summary>
    public static EntityProjectionRepositoryRegistry CreateDefault()
    {
        EntityProjectionRepositoryRegistry registry = new EntityProjectionRepositoryRegistry();
        registry.Register(new AutomationMeshStatusProjectedEntityRepository());
        registry.Register(new LocationProjectedEntityRepository());
        registry.Register(new UserProjectedEntityRepository());
        return registry;
    }

    /// <summary>
    /// Registers a projected entity repository.
    /// </summary>
    public void Register(IProjectedEntityRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (string.IsNullOrWhiteSpace(repository.Source))
            throw new ArgumentException("The projection repository source cannot be empty.", nameof(repository));

        if (EntityLogic.NormalizeEntityKinds(repository.SupportedKinds).Count == 0)
            throw new ArgumentException("The projection repository must support at least one kind.", nameof(repository));

        lock (_syncRoot)
        {
            if (_repositories.Contains(repository))
                return;

            _repositories.Add(repository);
        }
    }

    /// <summary>
    /// Gets the projected repositories that can serve at least one requested kind.
    /// </summary>
    public List<IProjectedEntityRepository> GetRepositoriesForKinds(IEnumerable<string> kinds)
    {
        HashSet<string> requestedKinds = new(EntityLogic.NormalizeEntityKinds(kinds), StringComparer.OrdinalIgnoreCase);
        if (requestedKinds.Count == 0)
            return [];

        lock (_syncRoot)
        {
            return [.. _repositories.Where(repository =>
                EntityLogic.NormalizeEntityKinds(repository.SupportedKinds)
                    .Any(supportedKind => requestedKinds.Contains(supportedKind)))];
        }
    }
}