using MaNoir.Core.Contracts.Models.Entities;

namespace MaNoir.Core.Entities;

/// <summary>
/// Implements the generic entity business logic layer on top of persistence helpers.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// EntityLogic logic = new EntityLogic();
/// Entity entity = await logic.UpsertAsync(new Entity()
/// {
///     Kind = "weather",
///     Id = "weather:paris"
/// }, cancellationToken);
/// </code>
/// <para>
/// The entity layer is the generic Core catalog for technical or projected entities that do not belong to a richer dedicated aggregate.
/// </para>
/// </remarks>
public sealed partial class EntityLogic
{
    private readonly EntityMongoOperations _mongoOperations;
    private readonly EntityProjectionRepositoryRegistry _projectionRepositoryRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityLogic"/> class.
    /// </summary>
    public EntityLogic()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityLogic"/> class with a projection registry.
    /// </summary>
    public EntityLogic(EntityProjectionRepositoryRegistry projectionRepositoryRegistry)
    {
        _mongoOperations = new EntityMongoOperations();
        _projectionRepositoryRegistry = projectionRepositoryRegistry ?? EntityProjectionRepositoryRegistry.CreateDefault();
    }
}