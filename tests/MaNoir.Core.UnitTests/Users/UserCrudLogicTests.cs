using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.UnitTests.Users;

[TestClass]
public sealed class UserCrudLogicTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void InitializeUser_ShouldNormalizeIdentifierAndClearGuestFields()
    {
        User user = new User()
        {
            IsGuest = true,
            DeleteAfter = DateTimeOffset.UtcNow
        };

        UserLogic.InitializeUser(user, "MCARBENAY");

        Assert.AreEqual("mcarbenay", user.Id);
        Assert.IsFalse(user.IsGuest);
        Assert.IsNull(user.DeleteAfter);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ApplyUserProfileUpdate_ShouldCopyEditableFields()
    {
        User target = new User()
        {
            Name = "Old",
            FirstName = "User"
        };
        User source = new User()
        {
            Name = "CARBENAY",
            FirstName = "Michael",
            CommonName = "Mike",
            MainEmail = "michael@example.test",
            MainPhoneNumber = "+33123456789",
            SsmlTaggedName = "<speak>Michael</speak>"
        };

        UserLogic.ApplyUserProfileUpdate(target, source);

        Assert.AreEqual("CARBENAY", target.Name);
        Assert.AreEqual("Michael", target.FirstName);
        Assert.AreEqual("Mike", target.CommonName);
        Assert.AreEqual("michael@example.test", target.MainEmail);
        Assert.AreEqual("+33123456789", target.MainPhoneNumber);
        Assert.AreEqual("<speak>Michael</speak>", target.SsmlTaggedName);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void CanDeleteMainUser_ShouldPreventDeletingLastMainUser()
    {
        List<User> mainUsers = new List<User>()
        {
            new User() { Id = "mcarbenay", IsMain = true }
        };

        bool canDelete = UserLogic.CanDeleteMainUser(mainUsers, "mcarbenay");

        Assert.IsFalse(canDelete);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SetUserIsMain_ShouldRejectGuests()
    {
        User guest = new User()
        {
            Id = "guest1",
            IsGuest = true
        };

        Assert.ThrowsException<InvalidOperationException>(() => UserLogic.SetUserIsMain(guest, true));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SetUserIsAdmin_ShouldRejectNonMainUsers()
    {
        User occasionalUser = new User()
        {
            Id = "friend1",
            IsGuest = false,
            IsMain = false
        };

        Assert.ThrowsException<InvalidOperationException>(() => UserLogic.SetUserIsAdmin(occasionalUser, true));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SetAvatar_ShouldReplaceAvatarWhenValuesChange()
    {
        User user = new User();
        UserImageData avatar = new UserImageData()
        {
            UrlSquareBig = "https://example.test/avatar-big.png",
            UrlSquareSmall = "https://example.test/avatar-small.png"
        };

        bool changed = UserLogic.SetAvatar(user, avatar);

        Assert.IsTrue(changed);
        Assert.AreEqual("https://example.test/avatar-big.png", user.Avatar.UrlSquareBig);
        Assert.AreEqual("https://example.test/avatar-small.png", user.Avatar.UrlSquareSmall);
    }
}