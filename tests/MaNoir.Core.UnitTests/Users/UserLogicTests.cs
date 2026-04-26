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

    [TestMethod]
    [TestCategory("Unit")]
    public void ApplyGuestProfileUpdate_ShouldCopyEditableGuestFields()
    {
        User target = new User()
        {
            Name = "Old",
            FirstName = "Guest"
        };
        User source = new User()
        {
            Name = "Dupont",
            FirstName = "Jean",
            CommonName = "Jean D.",
            MainEmail = "jean@example.test",
            MainPhoneNumber = "+33102030405"
        };

        UserLogic.ApplyGuestProfileUpdate(target, source);

        Assert.AreEqual("Dupont", target.Name);
        Assert.AreEqual("Jean", target.FirstName);
        Assert.AreEqual("Jean D.", target.CommonName);
        Assert.AreEqual("jean@example.test", target.MainEmail);
        Assert.AreEqual("+33102030405", target.MainPhoneNumber);
    }
}