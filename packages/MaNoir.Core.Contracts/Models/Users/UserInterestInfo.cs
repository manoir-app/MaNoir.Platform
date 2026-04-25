using MongoDB.Bson.Serialization.Attributes;

namespace MaNoir.Core.Contracts.Models.Users;

/// <summary>
/// Represents a structured interest attached to a user.
/// </summary>
public sealed class UserInterestInfo
{
    /// <summary>
    /// Gets or sets the interest identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the related user identifier.
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// Gets or sets the content data type.
    /// </summary>
    public string DataType { get; set; }
    /// <summary>
    /// Gets or sets the title or name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language { get; set; }
    /// <summary>
    /// Gets or sets the authors.
    /// </summary>
    public string[] Authors { get; set; }
    /// <summary>
    /// Gets or sets the actors.
    /// </summary>
    public string[] Actors { get; set; }
    /// <summary>
    /// Gets or sets the directors.
    /// </summary>
    public string[] Directors { get; set; }
    /// <summary>
    /// Gets or sets the producers.
    /// </summary>
    public string[] Producers { get; set; }
    /// <summary>
    /// Gets or sets the editors.
    /// </summary>
    public string[] Editors { get; set; }
    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    public string[] Genres { get; set; }
    /// <summary>
    /// Gets or sets the resolution or edition descriptor.
    /// </summary>
    public string Resolution { get; set; }
}