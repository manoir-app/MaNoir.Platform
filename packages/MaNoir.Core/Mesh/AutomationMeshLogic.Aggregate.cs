using MaNoir.Core.Contracts.Models.Mesh;
using System;

namespace MaNoir.Core.Mesh;

public sealed partial class AutomationMeshLogic
{
    /// <summary>
    /// Normalizes a mesh identifier for comparisons and persistence.
    /// </summary>
    /// <param name="meshId">The raw mesh identifier.</param>
    /// <returns>The normalized lower-case identifier, or <see langword="null"/> when missing.</returns>
    public static string NormalizeMeshId(string meshId)
    {
        if (string.IsNullOrWhiteSpace(meshId))
            return null;

        return meshId.ToLowerInvariant();
    }

    /// <summary>
    /// Determines whether a mesh identifier targets the local mesh.
    /// </summary>
    /// <param name="meshId">The mesh identifier to check.</param>
    /// <returns><see langword="true"/> when the identifier refers to the local mesh.</returns>
    public static bool IsLocalMesh(string meshId)
    {
        string normalizedMeshId = NormalizeMeshId(meshId);
        return string.Equals(normalizedMeshId, "local", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Ensures that a mesh instance exposes a public identifier.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <returns><see langword="true"/> when a new public identifier was assigned.</returns>
    public static bool EnsurePublicId(AutomationMesh mesh)
    {
        if (mesh == null)
            return false;

        if (!string.IsNullOrWhiteSpace(mesh.PublicId))
            return false;

        mesh.PublicId = Guid.NewGuid().ToString("D").ToLowerInvariant();
        return true;
    }

    /// <summary>
    /// Creates the default local mesh aggregate.
    /// </summary>
    /// <param name="machineName">The current machine name.</param>
    /// <param name="graphApiBaseUri">The base URI of the local graph API.</param>
    /// <returns>A newly initialized local mesh aggregate.</returns>
    public static AutomationMesh CreateLocalMesh(string machineName, string graphApiBaseUri)
    {
        AutomationMesh mesh = new AutomationMesh()
        {
            Id = "local",
            MainServer = new AutomationServer()
            {
                Id = machineName,
                Name = machineName,
                MainRole = new AutomationServerRole()
                {
                    Role = AutomationRole.GraphApi,
                    CommunicationMode = CommunicationMode.RestApi,
                    Uri = graphApiBaseUri
                }
            }
        };

        EnsurePublicId(mesh);
        return mesh;
    }

    /// <summary>
    /// Ensures that the local mesh exists and exposes a public identifier.
    /// </summary>
    /// <param name="mesh">The currently persisted local mesh, when available.</param>
    /// <param name="machineName">The current machine name.</param>
    /// <param name="graphApiBaseUri">The base URI of the local graph API.</param>
    /// <returns>The existing local mesh after repair, or a newly created local mesh.</returns>
    public static AutomationMesh EnsureLocalMesh(AutomationMesh mesh, string machineName, string graphApiBaseUri)
    {
        if (mesh == null)
            return CreateLocalMesh(machineName, graphApiBaseUri);

        EnsurePublicId(mesh);
        return mesh;
    }

    /// <summary>
    /// Associates a Manoir application account with a mesh and updates the main role URI.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="accountGuid">The Manoir account identifier.</param>
    /// <param name="name">The account display name.</param>
    /// <param name="prefix">The account domain prefix.</param>
    public static void AssociateAccount(AutomationMesh mesh, Guid accountGuid, string name, string prefix)
    {
        if (mesh == null)
            return;

        mesh.ManoirAppAccount = new AutomationMeshManoirAppAccount()
        {
            AccountGuid = accountGuid,
            Name = name,
            DomainPrefix = prefix
        };

        if (mesh.MainServer == null)
            mesh.MainServer = new AutomationServer();

        if (mesh.MainServer.MainRole == null)
            mesh.MainServer.MainRole = new AutomationServerRole();

        if (!string.IsNullOrWhiteSpace(prefix))
            mesh.MainServer.MainRole.Uri = "https://home." + prefix + ".manoir.app/";
    }
}