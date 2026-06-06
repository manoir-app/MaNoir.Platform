using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

/// <summary>
/// Maintains the persisted state of failed login attempts for platform security workflows.
/// </summary>
public sealed class UserFailedLoginStateTracker
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromHours(1);

    private readonly IMongoCollection<UserFailedLoginState> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserFailedLoginStateTracker"/> class.
    /// </summary>
    public UserFailedLoginStateTracker()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _collection = mongo.Database.GetCollection<UserFailedLoginState>("UserFailedLoginStates");
    }

    /// <summary>
    /// Registers one failed login attempt and returns the persisted state snapshot.
    /// </summary>
    public async Task<UserFailedLoginState> RegisterFailedLoginAttemptAsync(string attemptedUserId, string remoteAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = UserLogic.NormalizeUserId(attemptedUserId) ?? "unknown";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        UserFailedLoginState existingState = await GetAsync(normalizedUserId, cancellationToken);

        UserFailedLoginState state;
        if (existingState == null || existingState.LastFailedAtUtc == default || now - existingState.LastFailedAtUtc > FailureWindow)
        {
            state = new UserFailedLoginState()
            {
                UserId = normalizedUserId,
                FailedCount = 1,
                WindowStartedAtUtc = now,
                LastFailedAtUtc = now,
                LastRemoteAddress = remoteAddress?.Trim(),
                LastUserAgent = userAgent?.Trim(),
                LastAlertSentAtUtc = null
            };
        }
        else
        {
            state = existingState;
            state.FailedCount++;
            state.LastFailedAtUtc = now;
            state.LastRemoteAddress = remoteAddress?.Trim();
            state.LastUserAgent = userAgent?.Trim();
        }

        await SaveAsync(state, cancellationToken);
        return state;
    }

    /// <summary>
    /// Gets the failed login state of one user.
    /// </summary>
    public Task<UserFailedLoginState> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = UserLogic.NormalizeUserId(userId);
        if (normalizedUserId == null)
            return Task.FromResult<UserFailedLoginState>(null);

        return _collection.Find(state => state.UserId == normalizedUserId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Marks the last alert emission timestamp for one user.
    /// </summary>
    public async Task MarkAlertSentAsync(string userId, DateTimeOffset alertSentAtUtc, CancellationToken cancellationToken = default)
    {
        UserFailedLoginState state = await GetAsync(userId, cancellationToken);
        if (state == null)
            return;

        state.LastAlertSentAtUtc = alertSentAtUtc;
        await SaveAsync(state, cancellationToken);
    }

    private Task SaveAsync(UserFailedLoginState state, CancellationToken cancellationToken)
    {
        return _collection.ReplaceOneAsync(existing => existing.UserId == state.UserId, state, new ReplaceOptions() { IsUpsert = true }, cancellationToken);
    }
}

/// <summary>
/// Represents the persisted failed login state of one user for security monitoring.
/// </summary>
public sealed class UserFailedLoginState
{
    /// <summary>
    /// Gets or sets the canonical user identifier.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets the document identifier.
    /// </summary>
    public string Id => UserId;

    /// <summary>
    /// Gets or sets the number of failed logins in the current window.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the start of the current failure window.
    /// </summary>
    public DateTimeOffset WindowStartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last failed login.
    /// </summary>
    public DateTimeOffset LastFailedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last remote address seen for the failed login sequence.
    /// </summary>
    public string LastRemoteAddress { get; set; }

    /// <summary>
    /// Gets or sets the last user agent seen for the failed login sequence.
    /// </summary>
    public string LastUserAgent { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last alert emitted for this state.
    /// </summary>
    public DateTimeOffset? LastAlertSentAtUtc { get; set; }
}