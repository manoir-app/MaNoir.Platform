using MaNoir.Core.Contracts.Models.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Entities;

/// <summary>
/// Exposes read-only projected entities for one or more domain-owned kinds.
/// </summary>
public interface IProjectedEntityRepository
{
    /// <summary>
    /// Gets the logical source name used to flag projected entities as read-only.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets the kinds that can be projected by the repository.
    /// </summary>
    IReadOnlyCollection<string> SupportedKinds { get; }

    /// <summary>
    /// Gets one projected entity by kind and identifier.
    /// </summary>
    Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projected entities for the requested kinds.
    /// </summary>
    Task<List<Entity>> GetByKindsAsync(IReadOnlyCollection<string> kinds, CancellationToken cancellationToken = default);
}