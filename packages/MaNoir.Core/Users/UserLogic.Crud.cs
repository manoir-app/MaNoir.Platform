using MaNoir.Core.Contracts.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaNoir.Core.Users;

public sealed partial class UserLogic
{
    /// <summary>
    /// Normalizes a user identifier for comparisons and persistence.
    /// </summary>
    /// <param name="userId">The raw user identifier.</param>
    /// <returns>The normalized lower-case identifier, or <see langword="null"/> when missing.</returns>
    public static string NormalizeUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return userId.ToLowerInvariant();
    }

    /// <summary>
    /// Applies the editable profile fields of a non-guest user.
    /// </summary>
    /// <param name="target">The existing user to update.</param>
    /// <param name="source">The incoming user payload.</param>
    public static void ApplyUserProfileUpdate(User target, User source)
    {
        if (target == null || source == null)
            return;

        target.CommonName = source.CommonName;
        target.FirstName = source.FirstName;
        target.MainEmail = source.MainEmail;
        target.MainPhoneNumber = source.MainPhoneNumber;
        target.Name = source.Name;
        target.SsmlTaggedName = source.SsmlTaggedName;
    }

    /// <summary>
    /// Initializes a newly created non-guest user.
    /// </summary>
    /// <param name="user">The user to initialize.</param>
    /// <param name="userId">The canonical user identifier.</param>
    public static void InitializeUser(User user, string userId)
    {
        if (user == null)
            return;

        user.Id = NormalizeUserId(userId);
        user.IsGuest = false;
        user.DeleteAfter = null;
    }

    /// <summary>
    /// Determines whether a main user can be deleted.
    /// </summary>
    /// <param name="mainUsers">The current list of main users.</param>
    /// <param name="userId">The user identifier targeted for deletion.</param>
    /// <returns><see langword="true"/> when the main user can be deleted.</returns>
    public static bool CanDeleteMainUser(IReadOnlyCollection<User> mainUsers, string userId)
    {
        if (mainUsers == null)
            return false;

        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return false;

        int mainUserCount = mainUsers.Count;
        if (mainUserCount == 0)
            return false;

        if (mainUserCount > 1)
            return true;

        User mainUser = mainUsers.FirstOrDefault();
        return mainUser == null || !string.Equals(NormalizeUserId(mainUser.Id), normalizedUserId, StringComparison.InvariantCulture);
    }

    /// <summary>
    /// Applies the main user flag to a non-guest user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="isMain">The new main user flag.</param>
    /// <returns><see langword="true"/> when the flag changed.</returns>
    public static bool SetUserIsMain(User user, bool isMain)
    {
        if (user == null)
            return false;

        if (user.IsGuest)
            throw new InvalidOperationException("User is a guest.");

        if (user.IsMain == isMain)
            return false;

        user.IsMain = isMain;
        return true;
    }

    /// <summary>
    /// Replaces the avatar of a user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="avatar">The avatar payload to store.</param>
    /// <returns><see langword="true"/> when the avatar changed.</returns>
    public static bool SetAvatar(User user, UserImageData avatar)
    {
        if (user == null || avatar == null)
            return false;

        if (AreEquivalentAvatars(user.Avatar, avatar))
            return false;

        user.Avatar = avatar;
        return true;
    }

    private static bool AreEquivalentAvatars(UserImageData left, UserImageData right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        return string.Equals(left.UrlRoundBig, right.UrlRoundBig, StringComparison.InvariantCulture)
            && string.Equals(left.UrlRoundSmall, right.UrlRoundSmall, StringComparison.InvariantCulture)
            && string.Equals(left.UrlRoundTiny, right.UrlRoundTiny, StringComparison.InvariantCulture)
            && string.Equals(left.UrlRoundSvg, right.UrlRoundSvg, StringComparison.InvariantCulture)
            && string.Equals(left.UrlSquareBig, right.UrlSquareBig, StringComparison.InvariantCulture)
            && string.Equals(left.UrlSquareSmall, right.UrlSquareSmall, StringComparison.InvariantCulture)
            && string.Equals(left.UrlSquareTiny, right.UrlSquareTiny, StringComparison.InvariantCulture)
            && string.Equals(left.UrlSquareSvg, right.UrlSquareSvg, StringComparison.InvariantCulture);
    }
}