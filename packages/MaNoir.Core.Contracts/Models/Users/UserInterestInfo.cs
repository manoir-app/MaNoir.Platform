using MongoDB.Bson.Serialization.Attributes;

namespace MaNoir.Core.Contracts.Models.Users;

public sealed class UserInterestInfo
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }
    public string DataType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Language { get; set; }
    public string[] Authors { get; set; }
    public string[] Actors { get; set; }
    public string[] Directors { get; set; }
    public string[] Producers { get; set; }
    public string[] Editors { get; set; }
    public string[] Genres { get; set; }
    public string Resolution { get; set; }
}