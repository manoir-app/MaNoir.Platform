using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.DataAccess;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Users;

[TestClass]
[DoNotParallelize]
public sealed class UserNotificationPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task NotifyUserAsync_ShouldPersistInLegacyCollectionAndListNewestFirst()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        MongoDbHelper mongo = new MongoDbHelper();
        UserLogic bll = new UserLogic();

        await bll.NotifyUserAsync("MCarbenay", new UserNotification()
        {
            Date = new DateTimeOffset(2026, 4, 25, 9, 30, 0, TimeSpan.Zero),
            Title = "Oldest"
        });

        await bll.NotifyUserAsync("mcarbenay", new UserNotification()
        {
            Date = new DateTimeOffset(2026, 4, 25, 10, 30, 0, TimeSpan.Zero),
            Title = "Newest"
        });

        List<UserNotification> notifications = await bll.GetNotificationsAsync("MCARBENAY");
        BsonDocument storedDocument = await mongo.GetCollection("UserNotifications")
            .Find(new BsonDocument("UserId", "mcarbenay"))
            .FirstOrDefaultAsync();

        Assert.AreEqual(2, notifications.Count);
        Assert.AreEqual("Newest", notifications[0].Title);
        Assert.AreEqual("Oldest", notifications[1].Title);
        Assert.IsNotNull(storedDocument);
        Assert.AreEqual("mcarbenay", storedDocument["UserId"].AsString);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task NotifyUserAsync_ShouldDeduplicateBySourceAgentAndPreserveReadState()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        UserLogic bll = new UserLogic();

        UserNotification notification = await bll.NotifyUserAsync("mcarbenay", new UserNotification()
        {
            Date = new DateTimeOffset(2026, 4, 25, 10, 30, 0, TimeSpan.Zero),
            SourceAgent = "Aurore",
            SourceAgentNotificationId = "privacy:1",
            Title = "Privacy"
        });

        bool markedAsRead = await bll.MarkNotificationAsReadAsync("mcarbenay", notification.Id);

        UserNotification updatedNotification = await bll.NotifyUserAsync("MCARBENAY", new UserNotification()
        {
            Date = new DateTimeOffset(2026, 4, 25, 11, 30, 0, TimeSpan.Zero),
            SourceAgent = "AURORE",
            SourceAgentNotificationId = "privacy:1",
            Title = "Privacy updated"
        });

        List<UserNotification> notifications = await bll.GetNotificationsAsync("mcarbenay");

        Assert.IsTrue(markedAsRead);
        Assert.AreEqual(1, notifications.Count);
        Assert.AreEqual(notification.Id, updatedNotification.Id);
        Assert.AreEqual("Privacy updated", notifications[0].Title);
        Assert.IsTrue(notifications[0].IsRead);
        Assert.AreEqual("aurore", notifications[0].SourceAgent);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task NotifyUserAsync_ShouldAllowMobilePushPathWithoutExternalDependency()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        UserLogic bll = new UserLogic();

        UserNotification notification = await bll.NotifyUserAsync("mcarbenay", new UserNotification()
        {
            Date = new DateTimeOffset(2026, 4, 25, 12, 30, 0, TimeSpan.Zero),
            Title = "Mobile stub",
            Importance = UserNotificationImportance.Critical
        }, true);

        List<UserNotification> notifications = await bll.GetNotificationsAsync("mcarbenay");

        Assert.IsNotNull(notification);
        Assert.AreEqual(1, notifications.Count);
        Assert.AreEqual("Mobile stub", notifications[0].Title);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task MarkAllAndClearReadNotificationsAsync_ShouldApplyLegacyAgeThresholds()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        UserLogic bll = new UserLogic();
        DateTimeOffset now = DateTimeOffset.Now;

        await bll.NotifyUserAsync("mcarbenay", new UserNotification()
        {
            Date = now.AddMinutes(-2),
            Title = "Old read candidate"
        });

        await bll.NotifyUserAsync("mcarbenay", new UserNotification()
        {
            Date = now,
            Title = "Recent notification"
        });

        bool allMarkedAsRead = await bll.MarkAllNotificationsAsReadAsync("mcarbenay");
        List<UserNotification> afterMarkAll = await bll.GetNotificationsAsync("mcarbenay");
        bool cleared = await bll.ClearReadNotificationsAsync("mcarbenay");
        List<UserNotification> afterCleanup = await bll.GetNotificationsAsync("mcarbenay");

        Assert.IsTrue(allMarkedAsRead);
        Assert.IsTrue(cleared);
        Assert.AreEqual(2, afterMarkAll.Count);
        Assert.IsTrue(afterMarkAll[1].IsRead);
        Assert.IsFalse(afterMarkAll[0].IsRead);
        Assert.AreEqual(1, afterCleanup.Count);
        Assert.AreEqual("Recent notification", afterCleanup[0].Title);
    }
}