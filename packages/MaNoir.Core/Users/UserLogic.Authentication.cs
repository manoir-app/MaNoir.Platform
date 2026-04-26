using MaNoir.Core.Contracts.Models.Users;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

public sealed partial class UserLogic
{
    /// <summary>
    /// Authenticates one non-guest user against the stored password hash.
    /// </summary>
    public async Task<User> AuthenticateByPasswordAsync(string userId, string clearTextPassword, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null || string.IsNullOrWhiteSpace(clearTextPassword))
            return null;

        User user = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (user == null || user.IsGuest || string.IsNullOrWhiteSpace(user.HashedPassword))
            return null;

        return UserPasswordProtector.VerifyPassword(clearTextPassword, user.HashedPassword)
            ? user
            : null;
    }
}