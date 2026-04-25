using MaNoir.Core.Contracts.Models.Users;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

public sealed partial class UserLogic
{
    /// <summary>
    /// Sets a password hash for one existing non-guest user.
    /// </summary>
    public async Task<User> SetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            throw new InvalidUserCredentialsException();

        ValidateNewPassword(newPassword);

        User user = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (user == null || user.IsGuest)
            throw new InvalidUserCredentialsException();

        user.HashedPassword = UserPasswordProtector.HashPassword(newPassword);
        await SaveAsync(user, cancellationToken);
        return user;
    }

    /// <summary>
    /// Changes the password of one existing non-guest user after checking the current password.
    /// </summary>
    public async Task<User> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        User user = await AuthenticateByPasswordAsync(userId, currentPassword, cancellationToken);
        if (user == null)
            throw new InvalidUserCredentialsException();

        ValidateNewPassword(newPassword);

        user.HashedPassword = UserPasswordProtector.HashPassword(newPassword);
        await SaveAsync(user, cancellationToken);
        return user;
    }

    private static void ValidateNewPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new InvalidUserPasswordException("The new password cannot be empty.");

        if (newPassword.Trim().Length < 8)
            throw new InvalidUserPasswordException("The new password must contain at least 8 characters.");
    }
}