using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.DataAccess;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Users;

[TestClass]
[DoNotParallelize]
public sealed class UserPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task CreateUser_ShouldPersistLegacyCompatibleBsonShape()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        MongoDbHelper mongo = new MongoDbHelper();
        UserLogic bll = new UserLogic();

        User user = new User()
        {
            Id = "mcarbenay",
            DeleteAfter = null,
            IsGuest = false,
            IsMain = true,
            Name = "CARBENAY",
            FirstName = "Michael",
            CommonName = "Michael",
            SsmlTaggedName = null,
            HashedPassword = "hash-password",
            MainEmail = "michael@example.test",
            MainPhoneNumber = null,
            Routine = new RoutineData()
            {
                NextWakeUpTime = new DateTimeOffset(2026, 4, 25, 6, 30, 0, TimeSpan.Zero)
            },
            Avatar = new UserImageData()
            {
                UrlSquareBig = "https://example.test/users/avatars/mcarbenay/big-square.png",
                UrlSquareSmall = "https://example.test/users/avatars/mcarbenay/small-square.png",
                UrlSquareTiny = "https://example.test/users/avatars/mcarbenay/tiny-square.png"
            },
            HashedPinCode = "hash-pin"
        };

        user.Presence.LatestActivities.Add(new PresenceActivityData()
        {
            Date = new DateTimeOffset(2026, 1, 15, 8, 21, 14, TimeSpan.Zero),
            DeviceId = null,
            LocationId = "3552050b-e59a-4cf6-b67c-5503d7c2ba40",
            ActivityKind = "forcelocation",
            Status = "in"
        });

        await bll.SaveAsync(user);

        User storedUser = await bll.GetByIdAsync("mcarbenay");
        string collectionName = mongo.GetCollection<User>().CollectionNamespace.CollectionName;
        BsonDocument storedDocument = await mongo.GetCollection(collectionName).Find(new BsonDocument("_id", "mcarbenay")).FirstOrDefaultAsync();

        Assert.IsNotNull(storedUser);
        Assert.IsNotNull(storedDocument);
        Assert.AreEqual("Michael", storedUser.CommonName);
        Assert.AreEqual("hash-pin", storedUser.HashedPinCode);
        Assert.AreEqual("https://example.test/users/avatars/mcarbenay/big-square.png", storedUser.Avatar.UrlSquareBig);
        Assert.AreEqual("mcarbenay", storedDocument["_id"].AsString);
        Assert.IsTrue(storedDocument["DeleteAfter"].IsBsonNull, storedDocument.ToJson());
        Assert.IsTrue(storedDocument["Presence"]["LatestActivities"][0]["Date"].IsBsonArray, storedDocument.ToJson());
        Assert.IsTrue(storedDocument["Routine"]["NextWakeUpTime"].IsBsonArray, storedDocument.ToJson());
        Assert.AreEqual(0, storedDocument["HealthData"]["WeightDatas"].AsBsonArray.Count);
        Assert.AreEqual(0, storedDocument["Presence"]["Location"].AsBsonArray.Count);
    }
}