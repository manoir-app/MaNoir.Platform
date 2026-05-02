namespace MaNoir.Core.Contracts.Models.Mesh;

/// <summary>
/// Represents a frontend URL upsert request for the local mesh catalog.
/// </summary>
public sealed class AutomationMeshFrontendUrlUpsertRequest
{
    /// <summary>
    /// Gets or sets the absolute frontend URL to store.
    /// </summary>
    public string Url { get; set; }
}