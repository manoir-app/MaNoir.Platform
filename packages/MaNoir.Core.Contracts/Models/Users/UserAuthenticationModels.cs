using System;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents one user login request.
/// </summary>
public sealed class UserLoginRequest
{
    /// <summary>
    /// Gets or sets the canonical user identifier.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the clear text password supplied by the caller.
    /// </summary>
    public string Password { get; set; }
}

/// <summary>
/// Represents one successful authenticated user session.
/// </summary>
public sealed class UserAuthenticationResponse
{
    /// <summary>
    /// Gets or sets the token type.
    /// </summary>
    public string TokenType { get; set; }

    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiration timestamp.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user projection.
    /// </summary>
    public User User { get; set; }
}

/// <summary>
/// Represents one authenticated password change request for the current user.
/// </summary>
public sealed class UserChangePasswordRequest
{
    /// <summary>
    /// Gets or sets the current clear text password.
    /// </summary>
    public string CurrentPassword { get; set; }

    /// <summary>
    /// Gets or sets the new clear text password.
    /// </summary>
    public string NewPassword { get; set; }
}