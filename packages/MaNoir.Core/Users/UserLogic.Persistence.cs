using MaNoir.Core.Contracts.Models.Users;
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
    /// Saves a user aggregate.
    /// </summary>
    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        return _mongoOperations.SaveAsync(user, cancellationToken);
    }
}