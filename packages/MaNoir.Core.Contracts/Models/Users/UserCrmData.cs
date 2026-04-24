using MongoDB.Bson.Serialization.Attributes;

namespace MaNoir.Core.Contracts.Models.Users;

public sealed class UserCrmData
{
    [BsonId]
    public string Id { get; set; }

    public string DataOwnerUserId { get; set; }
    public string SubjectUserId { get; set; }
    public string Summary { get; set; }
}