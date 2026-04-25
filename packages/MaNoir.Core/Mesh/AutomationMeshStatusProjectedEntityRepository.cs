using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Mesh;

/// <summary>
/// Projects automation mesh status information as read-only entities.
/// </summary>
public sealed class AutomationMeshStatusProjectedEntityRepository : IProjectedEntityRepository
{
    /// <inheritdoc/>
    public string Source => "mesh/status";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> SupportedKinds => [CoreEntityConstants.Kinds.Status];

    /// <inheritdoc/>
    public async Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default)
    {
        string normalizedKind = EntityLogic.NormalizeEntityKind(kind);
        string normalizedEntityId = EntityLogic.NormalizeEntityId(entityId);
        if (normalizedKind != CoreEntityConstants.Kinds.Status || normalizedEntityId == null)
            return null;

        AutomationMesh mesh = await new AutomationMeshLogic().GetByIdAsync(normalizedEntityId, cancellationToken);
        return ToProjectedEntity(mesh);
    }

    /// <inheritdoc/>
    public async Task<List<Entity>> GetByKindsAsync(IReadOnlyCollection<string> kinds, CancellationToken cancellationToken = default)
    {
        List<string> normalizedKinds = EntityLogic.NormalizeEntityKinds(kinds);
        if (!normalizedKinds.Contains(CoreEntityConstants.Kinds.Status))
            return [];

        AutomationMesh localMesh = await new AutomationMeshLogic().GetLocalAsync(cancellationToken);
        Entity entity = ToProjectedEntity(localMesh);
        if (entity == null)
            return [];

        return [entity];
    }

    private static Entity ToProjectedEntity(AutomationMesh mesh)
    {
        if (mesh == null)
            return null;

        AutomationMeshStatus status = mesh.Status ?? new AutomationMeshStatus();
        Entity entity = new Entity()
        {
            Id = AutomationMeshLogic.NormalizeMeshId(mesh.Id),
            EntityKind = CoreEntityConstants.Kinds.Status,
            Name = string.IsNullOrWhiteSpace(mesh.PublicId) ? "Mesh status" : mesh.PublicId,
            MeshId = AutomationMeshLogic.NormalizeMeshId(mesh.Id),
            LocationId = mesh.LocationId,
            Datas =
            {
                ["GeneralStatusCode"] = new EntityData()
                {
                    SimpleType = "System.String",
                    SimpleValue = status.GeneralStatusCode,
                    Category = CoreEntityConstants.Categories.Diagnostic
                },
                ["InternetConnectionStatusCode"] = new EntityData()
                {
                    SimpleType = "System.String",
                    SimpleValue = status.InternetConnectionStatusCode,
                    Category = CoreEntityConstants.Categories.Diagnostic
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(mesh.CurrentScenario))
        {
            entity.Datas["CurrentScenario"] = new EntityData()
            {
                SimpleType = "System.String",
                SimpleValue = mesh.CurrentScenario,
                Category = CoreEntityConstants.Categories.Configuration
            };
        }

        entity.Datas["PrivacyModeLabel"] = new EntityData()
        {
            SimpleType = "System.String",
            SimpleValue = GetPrivacyModeLabel(mesh),
            Category = CoreEntityConstants.Categories.Configuration
        };

        entity.Datas["IsPrivacyModeEnabled"] = new EntityData()
        {
            SimpleType = "System.Boolean",
            SimpleValue = AutomationMeshLogic.IsPrivacyModeEnabled(mesh) ? "true" : "false",
            Category = CoreEntityConstants.Categories.Configuration
        };

        if (mesh.CurrentPrivacyMode.HasValue)
        {
            entity.Datas["CurrentPrivacyMode"] = new EntityData()
            {
                SimpleType = "System.String",
                SimpleValue = mesh.CurrentPrivacyMode.Value.ToString(),
                Category = CoreEntityConstants.Categories.Configuration
            };
        }

        if (!string.IsNullOrWhiteSpace(mesh.MainSsid))
        {
            entity.Datas["MainSsid"] = new EntityData()
            {
                SimpleType = "System.String",
                SimpleValue = mesh.MainSsid,
                Category = CoreEntityConstants.Categories.Configuration
            };
        }

        return entity;
    }

    private static string GetPrivacyModeLabel(AutomationMesh mesh)
    {
        return mesh?.CurrentPrivacyMode.HasValue == true
            ? mesh.CurrentPrivacyMode.Value.ToString()
            : "none";
    }
}