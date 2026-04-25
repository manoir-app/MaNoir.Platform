using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Users;

[TestClass]
[DoNotParallelize]
public sealed class UserCrudPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task UserCrudMethods_ShouldPersistUserLifecycleRules()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        UserLogic logic = new UserLogic();

        User createdMainUser = await logic.UpsertUserAsync("MCARBENAY", new User()
        {
            Name = "CARBENAY",
            FirstName = "Michael",
            CommonName = "Michael",
            MainEmail = "michael@example.test",
            IsMain = true
        });

        User createdSecondaryUser = await logic.UpsertUserAsync("ADELCOURT", new User()
        {
            Name = "DELCOURT",
            FirstName = "Alice",
            CommonName = "Alice",
            MainEmail = "alice@example.test",
            IsMain = false
        });

        User updatedMainUser = await logic.UpsertUserAsync("mcarbenay", new User()
        {
            Name = "CARBENAY",
            FirstName = "Micha",
            CommonName = "Micha",
            MainEmail = "micha@example.test",
            SsmlTaggedName = "<speak>Micha</speak>"
        });

        User changedSecondaryUser = await logic.ChangeUserIsMainAsync("adelcourt", true);
        bool deletedLastMainUser = await logic.DeleteMainUserAsync("mcarbenay");
        bool deletedOtherUser = await logic.DeleteOtherUserAsync("mcarbenay");

        User storedSecondaryUser = await logic.GetByIdAsync("adelcourt");
        User deletedMainUser = await logic.GetByIdAsync("mcarbenay");

        Assert.IsNotNull(createdMainUser);
        Assert.IsNotNull(createdSecondaryUser);
        Assert.IsNotNull(updatedMainUser);
        Assert.IsNotNull(changedSecondaryUser);
        Assert.AreEqual("mcarbenay", createdMainUser.Id);
        Assert.AreEqual("adelcourt", createdSecondaryUser.Id);
        Assert.AreEqual("Micha", updatedMainUser.CommonName);
        Assert.AreEqual("micha@example.test", updatedMainUser.MainEmail);
        Assert.IsTrue(changedSecondaryUser.IsMain);
        Assert.IsTrue(deletedLastMainUser);
        Assert.IsTrue(deletedOtherUser);
        Assert.IsNotNull(storedSecondaryUser);
        Assert.IsTrue(storedSecondaryUser.IsMain);
        Assert.IsNull(deletedMainUser);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GuestCrudMethods_ShouldPersistGuestLifecycleRules()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        UserLogic logic = new UserLogic();

        User createdGuest = await logic.UpsertGuestUserAsync(new User()
        {
            Name = "Dupont",
            FirstName = "Jean-Michel",
            CommonName = "Jean",
            MainEmail = "jean@example.test"
        });

        User updatedGuest = await logic.UpsertGuestUserAsync(new User()
        {
            Id = "dupontjeanmichel",
            Name = "Dupont",
            FirstName = "Jean",
            CommonName = "Jean D.",
            MainEmail = "jean.dupont@example.test"
        });

        bool deletedGuest = await logic.DeleteOtherUserAsync("dupontjeanmichel");

        System.Collections.Generic.List<User> guestUsers = await logic.GetGuestUsersAsync();
        User deletedUser = await logic.GetByIdAsync("dupontjeanmichel");

        Assert.IsNotNull(createdGuest);
        Assert.IsNotNull(updatedGuest);
        Assert.AreEqual("dupontjeanmichel", createdGuest.Id);
        Assert.IsTrue(createdGuest.IsGuest);
        Assert.IsFalse(createdGuest.IsMain);
        Assert.AreEqual("Jean D.", updatedGuest.CommonName);
        Assert.AreEqual("jean.dupont@example.test", updatedGuest.MainEmail);
        Assert.IsTrue(deletedGuest);
        Assert.AreEqual(0, guestUsers.Count);
        Assert.IsNull(deletedUser);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task UpdateAvatarAsync_ShouldPersistAvatarChanges()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        UserLogic logic = new UserLogic();

        await logic.UpsertUserAsync("mcarbenay", new User()
        {
            Name = "CARBENAY",
            FirstName = "Michael",
            CommonName = "Michael"
        });

        User updatedUser = await logic.UpdateAvatarAsync("MCARBENAY", new UserImageData()
        {
            UrlSquareBig = "https://example.test/users/avatars/mcarbenay/big-square.png",
            UrlSquareSmall = "https://example.test/users/avatars/mcarbenay/small-square.png"
        });

        User storedUser = await logic.GetByIdAsync("mcarbenay");

        Assert.IsNotNull(updatedUser);
        Assert.IsNotNull(storedUser);
        Assert.AreEqual("https://example.test/users/avatars/mcarbenay/big-square.png", storedUser.Avatar.UrlSquareBig);
        Assert.AreEqual("https://example.test/users/avatars/mcarbenay/small-square.png", storedUser.Avatar.UrlSquareSmall);
    }
}