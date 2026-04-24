using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MaNoir.Core.Contracts.Models.Users;

public sealed class User
{
    public User()
    {
        HealthData = new HealthData();
        Presence = new PresenceData();
        Avatar = new UserImageData();
    }

    public void MinimizeData()
    {
        HealthData = null;
        HashedPassword = null;
        Presence = null;
    }

    public void ForPresence()
    {
        HealthData = null;
        HashedPassword = null;
    }

    [BsonId]
    public string Id { get; set; }

    public DateTimeOffset? DeleteAfter { get; set; }
    public bool IsGuest { get; set; }
    public bool IsMain { get; set; }
    public string Name { get; set; }
    public string FirstName { get; set; }
    public string CommonName { get; set; }
    public string SsmlTaggedName { get; set; }
    public string HashedPinCode { get; set; }
    public string HashedPassword { get; set; }
    public string MainEmail { get; set; }
    public string MainPhoneNumber { get; set; }
    public HealthData HealthData { get; set; }
    public PresenceData Presence { get; set; }
    public RoutineData Routine { get; set; }
    public UserImageData Avatar { get; set; }
}

public sealed class UserImageData
{
    public string UrlRoundBig { get; set; }
    public string UrlRoundSmall { get; set; }
    public string UrlRoundTiny { get; set; }
    public string UrlRoundSvg { get; set; }
    public string UrlSquareBig { get; set; }
    public string UrlSquareSmall { get; set; }
    public string UrlSquareTiny { get; set; }
    public string UrlSquareSvg { get; set; }
}