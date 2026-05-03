namespace MaNoir.Core.Contracts.Models.Mesh;

/// <summary>
/// Represents the public local mesh settings exposed to technical consumers.
/// </summary>
public sealed class AutomationMeshLocalSettings
{
    /// <summary>
    /// Gets or sets the mesh identifier.
    /// </summary>
    public string MeshId { get; set; }

    /// <summary>
    /// Gets or sets the public mesh identifier.
    /// </summary>
    public string PublicId { get; set; }

    /// <summary>
    /// Gets or sets the public base domain currently configured on the mesh.
    /// </summary>
    public string PublicBaseDomain { get; set; }

    /// <summary>
    /// Gets or sets the default mesh language identifier.
    /// </summary>
    public string LanguageId { get; set; }

    /// <summary>
    /// Gets or sets the default mesh IANA time zone identifier.
    /// </summary>
    public string TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets the default mesh country identifier.
    /// </summary>
    public string CountryId { get; set; }
}