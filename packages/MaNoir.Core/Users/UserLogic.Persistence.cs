using MaNoir.Core.Contracts.Models.Users;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

public sealed partial class UserLogic
{
    private readonly UserMongoOperations _mongoOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserLogic"/> class.
    /// </summary>
    public UserLogic()
    {
        _mongoOperations = new UserMongoOperations();
    }

    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    public Task<User> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetByIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Lists the main household users.
    /// </summary>
    public Task<List<User>> GetMainUsersAsync(CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetMainUsersAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all users.
    /// </summary>
    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all guest users.
    /// </summary>
    public Task<List<User>> GetGuestUsersAsync(CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetGuestUsersAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all non-guest users.
    /// </summary>
    public Task<List<User>> GetNonGuestUsersAsync(CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetNonGuestUsersAsync(cancellationToken);
    }

    /// <summary>
    /// Saves a user aggregate.
    /// </summary>
    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        return _mongoOperations.SaveAsync(user, cancellationToken);
    }

    /// <summary>
    /// Creates or updates a non-guest user and persists the change.
    /// </summary>
    public async Task<User> UpsertUserAsync(string userId, User user, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null || user == null)
            return null;

        User existingUser = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (existingUser != null)
        {
            ApplyUserProfileUpdate(existingUser, user);
            await SaveAsync(existingUser, cancellationToken);
            return existingUser;
        }

        InitializeUser(user, normalizedUserId);
        await SaveAsync(user, cancellationToken);
        return user;
    }

    /// <summary>
    /// Creates or updates a guest user and persists the change.
    /// </summary>
    public async Task<User> UpsertGuestUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            return null;

        string normalizedUserId = GetGuestUserId(user);
        if (normalizedUserId == null)
            return null;

        User existingUser = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (existingUser != null)
        {
            if (!existingUser.IsGuest)
                return null;

            ApplyGuestProfileUpdate(existingUser, user);
            await SaveAsync(existingUser, cancellationToken);
            return existingUser;
        }

        InitializeGuestUser(user, System.DateTimeOffset.Now);
        await SaveAsync(user, cancellationToken);
        return user;
    }

    /// <summary>
    /// Deletes a main user when allowed.
    /// </summary>
    public async Task<bool> DeleteMainUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return true;

        List<User> mainUsers = await GetMainUsersAsync(cancellationToken);
        if (!CanDeleteMainUser(mainUsers, normalizedUserId))
            return false;

        User existingUser = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (existingUser == null || !existingUser.IsMain)
            return false;

        DeleteResult deleteResult = await _mongoOperations.DeleteAsync(normalizedUserId, cancellationToken);
        return deleteResult.DeletedCount == 1;
    }

    /// <summary>
    /// Deletes a non-main user.
    /// </summary>
    public async Task<bool> DeleteOtherUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return true;

        User existingUser = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (existingUser == null)
            return true;

        if (existingUser.IsMain)
            return false;

        DeleteResult deleteResult = await _mongoOperations.DeleteAsync(normalizedUserId, cancellationToken);
        return deleteResult.DeletedCount == 1;
    }

    /// <summary>
    /// Changes the main user flag of a non-guest user and persists the change.
    /// </summary>
    public async Task<User> ChangeUserIsMainAsync(string userId, bool isMain, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return null;

        User existingUser = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (existingUser == null)
            return null;

        bool changed = SetUserIsMain(existingUser, isMain);
        if (changed)
            await SaveAsync(existingUser, cancellationToken);

        return existingUser;
    }

    /// <summary>
    /// Updates the avatar of a user and persists the change when needed.
    /// </summary>
    public async Task<User> UpdateAvatarAsync(string userId, UserImageData avatar, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = NormalizeUserId(userId);
        if (normalizedUserId == null)
            return null;

        User existingUser = await GetByIdAsync(normalizedUserId, cancellationToken);
        if (existingUser == null)
            return null;

        bool changed = SetAvatar(existingUser, avatar);
        if (changed)
            await SaveAsync(existingUser, cancellationToken);

        return existingUser;
    }
}