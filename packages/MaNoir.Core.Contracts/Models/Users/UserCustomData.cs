using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Users;

public sealed class UserCustomData
{
    [BsonId]
    public string Id { get; set; }

    public string Code { get; set; }
    public string UserId { get; set; }
    public string EncryptedData { get; set; }
    public string EncryptionMode { get; set; }
    public Dictionary<string, string> PropertyBag { get; set; }
}