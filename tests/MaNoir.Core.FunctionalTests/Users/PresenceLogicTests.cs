using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Mesh;

namespace MaNoir.Core.FunctionalTests.Users;

[TestClass]
public sealed class PresenceLogicTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task HandleActivityAndMaintenance_ShouldPersistPresenceAndEnablePrivacyForGuests()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.UpsertUserAsync("john", new User() { FirstName = "John", Name = "Doe", CommonName = "John", IsMain = true });
        User guestUser = await userLogic.UpsertGuestUserAsync(new User() { FirstName = "Jane", Name = "Guest", CommonName = "Jane", MainEmail = "jane@example.net" });

        AutomationMeshLogic meshLogic = new AutomationMeshLogic();
        await meshLogic.GetOrCreateLocalAsync("tests-host", "http://localhost", default);

        PresenceLogic presenceLogic = new PresenceLogic();

        PresenceChangeSet johnChangeSet = await presenceLogic.HandleActivityAsync(new PresenceNotificationData()
        {
            AssociatedUser = "john",
            ActivityKind = "personaldevicedetection",
            LocationId = "home",
            Status = "in",
            Date = DateTimeOffset.UtcNow
        }, "home");
        User updatedJohn = johnChangeSet.UpdatedUser;

        Assert.IsNotNull(updatedJohn);
        Assert.AreEqual(1, updatedJohn.Presence.Location.Count);
        Assert.AreEqual("home", updatedJohn.Presence.Location[0].LocationId);
        Assert.AreEqual(60, updatedJohn.Presence.Location[0].Probability);
        CollectionAssert.AreEqual(new[] { "john" }, johnChangeSet.NewlyPresentUserIds);

        PresenceChangeSet guestChangeSet = await presenceLogic.HandleActivityAsync(new PresenceNotificationData()
        {
            AssociatedUser = guestUser.Id,
            ActivityKind = "forcelocation",
            LocationId = "home",
            Status = "in",
            Date = DateTimeOffset.UtcNow
        }, "home");
        User updatedGuest = guestChangeSet.UpdatedUser;

        Assert.IsNotNull(updatedGuest);
        CollectionAssert.AreEqual(new[] { guestUser.Id }, guestChangeSet.NewlyPresentUserIds);

        PresenceChangeSet maintenanceChangeSet = await presenceLogic.RunMaintenanceAsync();
        Assert.IsFalse(maintenanceChangeSet.HasChanges);

        AutomationMesh mesh = await meshLogic.GetLocalAsync();
        Assert.IsNotNull(mesh);
        Assert.AreEqual(AutomationMeshPrivacyMode.High, mesh.CurrentPrivacyMode);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task RunMaintenance_ShouldReportUsersBecomingAbsentWhenProbabilityDropsBelowThreshold()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        User user = await userLogic.UpsertUserAsync("john", new User() { FirstName = "John", Name = "Doe", CommonName = "John", IsMain = true });
        user.Presence.Location.Add(new PresenceLocationData()
        {
            LocationId = "home",
            Probability = 50,
            LatestUpdate = DateTimeOffset.UtcNow.AddMinutes(-16)
        });
        await userLogic.SaveAsync(user);

        PresenceLogic presenceLogic = new PresenceLogic();

        PresenceChangeSet changeSet = await presenceLogic.RunMaintenanceAsync();

        Assert.IsTrue(changeSet.HasChanges);
        CollectionAssert.AreEqual(new[] { "john" }, changeSet.NewlyAbsentUserIds);

        User storedUser = await userLogic.GetByIdAsync("john");
        Assert.IsNotNull(storedUser);
        Assert.AreEqual(45, storedUser.Presence.Location[0].Probability);
    }
}