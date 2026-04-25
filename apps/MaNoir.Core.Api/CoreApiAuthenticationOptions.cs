namespace MaNoir.Core.Api;

/// <summary>
/// Defines the JWT authentication settings used by the Core API.
/// </summary>
public sealed class CoreApiAuthenticationOptions
{
    public const string ConfigurationSectionName = "MaNoir:Authentication:UsersJwt";
    public const string DevelopmentSigningKey = "development-only-manoir-users-jwt-signing-key-2026";

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    public string Issuer { get; set; } = "manoir.core";

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    public string Audience { get; set; } = "manoir.core.users";

    /// <summary>
    /// Gets or sets the JWT signing key.
    /// </summary>
    public string SigningKey { get; set; }

    /// <summary>
    /// Gets or sets the access token lifetime in minutes.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 720;

    /// <summary>
    /// Gets or sets the cookie name used to persist the user token.
    /// </summary>
    public string CookieName { get; set; } = "manoir_users_access_token";
}