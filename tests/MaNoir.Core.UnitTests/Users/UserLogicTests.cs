using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MaNoir.Core.UnitTests.Users;

[TestClass]
public sealed class UserLogicTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void GetGuestUserId_ShouldUseSanitizedNamesAndLowerCase()
    {
        User user = new User()
        {
            Name = "Dupont",
            FirstName = "Jean-Michel"
        };

        string guestUserId = UserLogic.GetGuestUserId(user);

        Assert.AreEqual("dupontjeanmichel", guestUserId);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void InitializeGuestUser_ShouldApplyGuestDefaults()
    {
        User user = new User()
        {
            MainEmail = "Guest.User@Example.com"
        };
        DateTimeOffset now = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);

        UserLogic.InitializeGuestUser(user, now);

        Assert.IsTrue(user.IsGuest);
        Assert.IsFalse(user.IsMain);
        Assert.AreEqual(now.AddDays(1), user.DeleteAfter);
        Assert.AreEqual("guestuserexamplecom", user.Id);
    }
}