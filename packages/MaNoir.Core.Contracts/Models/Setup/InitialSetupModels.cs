using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Users;

namespace MaNoir.Core.Contracts.Models.Setup;

/// <summary>
/// Represents the current first-setup availability of the local Core instance.
/// </summary>
public sealed class InitialSetupStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether the first setup can still be executed.
    /// </summary>
    public bool CanInitialize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the local mesh already exists.
    /// </summary>
    public bool HasMesh { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether at least one user already exists.
    /// </summary>
    public bool HasUsers { get; set; }
}

/// <summary>
/// Represents the payload required to initialize the local Core database.
/// </summary>
public sealed class InitialSetupRequest
{
    /// <summary>
    /// Gets or sets the master admin user identifier.
    /// </summary>
    public string AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the master admin first name.
    /// </summary>
    public string AdminFirstName { get; set; }

    /// <summary>
    /// Gets or sets the master admin last name.
    /// </summary>
    public string AdminName { get; set; }

    /// <summary>
    /// Gets or sets the preferred display name of the master admin.
    /// </summary>
    public string AdminCommonName { get; set; }

    /// <summary>
    /// Gets or sets the main email of the master admin.
    /// </summary>
    public string AdminEmail { get; set; }

    /// <summary>
    /// Gets or sets the initial master admin password.
    /// </summary>
    public string AdminPassword { get; set; }

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

    /// <summary>
    /// Gets or sets the default public base domain of the mesh.
    /// </summary>
    public string PublicBaseDomain { get; set; }
}

/// <summary>
/// Represents the outcome of the first setup initialization.
/// </summary>
public sealed class InitialSetupResponse
{
    /// <summary>
    /// Gets or sets the created local mesh.
    /// </summary>
    public AutomationMesh Mesh { get; set; }

    /// <summary>
    /// Gets or sets the created master admin user projection.
    /// </summary>
    public User User { get; set; }
}