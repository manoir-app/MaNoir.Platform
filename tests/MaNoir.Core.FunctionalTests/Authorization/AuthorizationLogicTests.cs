using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Authorization;

[TestClass]
public sealed class AuthorizationLogicTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetEffectiveAccessLevelAsync_ShouldApplyParentZoneGrantToChildZones()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false });

        AuthorizationLogic logic = new AuthorizationLogic();
        await logic.ReplaceUserAuthorizationAsync("sarah",
        [
            new UserZoneAccess() { ZoneId = CoreAccessZones.CoreAdminUi, Level = AccessLevel.Read }
        ]);

        AccessLevel level = await logic.GetEffectiveAccessLevelAsync("sarah", "core.admin-ui.settings");

        Assert.AreEqual(AccessLevel.Read, level);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetEffectiveAccessLevelAsync_ShouldGrantManageEverywhereToAdmins()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false, IsMain = true, IsAdmin = true });

        AuthorizationLogic logic = new AuthorizationLogic();
        AccessLevel level = await logic.GetEffectiveAccessLevelAsync("sarah", "daily.weather");

        Assert.AreEqual(AccessLevel.Manage, level);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetEffectiveAccessLevelAsync_ShouldNotGrantManageEverywhereToMainNonAdmins()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false, IsMain = true, IsAdmin = false });

        AuthorizationLogic logic = new AuthorizationLogic();
        AccessLevel level = await logic.GetEffectiveAccessLevelAsync("sarah", "daily.weather");

        Assert.AreEqual(AccessLevel.None, level);
    }
}