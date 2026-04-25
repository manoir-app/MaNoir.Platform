using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents a user managed by the MaNoir platform.
/// </summary>
public sealed class User
{
    public User()
    {
        HealthData = new HealthData();
        Presence = new PresenceData();
        Avatar = new UserImageData();
    }

    /// <summary>
    /// Gets or sets the canonical user identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the expiration date for temporary users.
    /// </summary>
    public DateTimeOffset? DeleteAfter { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the user is a guest.
    /// </summary>
    public bool IsGuest { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the user is a main household user.
    /// </summary>
    public bool IsMain { get; set; }
    /// <summary>
    /// Gets or sets the last name or primary name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string FirstName { get; set; }
    /// <summary>
    /// Gets or sets the preferred common display name.
    /// </summary>
    public string CommonName { get; set; }
    /// <summary>
    /// Gets or sets the SSML-friendly display name.
    /// </summary>
    public string SsmlTaggedName { get; set; }
    /// <summary>
    /// Gets or sets the hashed PIN code.
    /// </summary>
    public string HashedPinCode { get; set; }
    /// <summary>
    /// Gets or sets the hashed password.
    /// </summary>
    public string HashedPassword { get; set; }
    /// <summary>
    /// Gets or sets the main email address.
    /// </summary>
    public string MainEmail { get; set; }
    /// <summary>
    /// Gets or sets the main phone number.
    /// </summary>
    public string MainPhoneNumber { get; set; }
    /// <summary>
    /// Gets or sets the health data.
    /// </summary>
    public HealthData HealthData { get; set; }
    /// <summary>
    /// Gets or sets the presence data.
    /// </summary>
    public PresenceData Presence { get; set; }
    /// <summary>
    /// Gets or sets the routine data.
    /// </summary>
    public RoutineData Routine { get; set; }
    /// <summary>
    /// Gets or sets the avatar information.
    /// </summary>
    public UserImageData Avatar { get; set; }
}

/// <summary>
/// Represents user avatar URLs for multiple shapes and sizes.
/// </summary>
public sealed class UserImageData
{
    /// <summary>
    /// Gets or sets the large round avatar URL.
    /// </summary>
    public string UrlRoundBig { get; set; }
    /// <summary>
    /// Gets or sets the small round avatar URL.
    /// </summary>
    public string UrlRoundSmall { get; set; }
    /// <summary>
    /// Gets or sets the tiny round avatar URL.
    /// </summary>
    public string UrlRoundTiny { get; set; }
    /// <summary>
    /// Gets or sets the SVG round avatar URL.
    /// </summary>
    public string UrlRoundSvg { get; set; }
    /// <summary>
    /// Gets or sets the large square avatar URL.
    /// </summary>
    public string UrlSquareBig { get; set; }
    /// <summary>
    /// Gets or sets the small square avatar URL.
    /// </summary>
    public string UrlSquareSmall { get; set; }
    /// <summary>
    /// Gets or sets the tiny square avatar URL.
    /// </summary>
    public string UrlSquareTiny { get; set; }
    /// <summary>
    /// Gets or sets the SVG square avatar URL.
    /// </summary>
    public string UrlSquareSvg { get; set; }
}