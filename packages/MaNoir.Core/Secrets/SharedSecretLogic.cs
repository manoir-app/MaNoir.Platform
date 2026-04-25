using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Secrets;

/// <summary>
/// Implements local shared secret persistence protected by environment-backed encryption material.
/// </summary>
public sealed class SharedSecretLogic
{
    private readonly SharedSecretMongoOperations _mongoOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedSecretLogic"/> class.
    /// </summary>
    public SharedSecretLogic()
    {
        _mongoOperations = new SharedSecretMongoOperations();
    }

    /// <summary>
    /// Stores or updates one shared secret.
    /// </summary>
    public async Task SetSecretAsync(string secretId, string clearText, CancellationToken cancellationToken = default)
    {
        string normalizedSecretId = NormalizeSecretId(secretId);
        if (normalizedSecretId == null)
            throw new ArgumentException("The shared secret identifier cannot be empty.", nameof(secretId));

        if (clearText == null)
            throw new ArgumentNullException(nameof(clearText));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        SharedSecret existingSecret = await _mongoOperations.GetByIdAsync(normalizedSecretId, cancellationToken);
        SharedSecret protectedSecret = SharedSecretProtector.Protect(clearText);
        protectedSecret.Id = normalizedSecretId;
        protectedSecret.CreatedAtUtc = existingSecret?.CreatedAtUtc == default || existingSecret == null ? now : existingSecret.CreatedAtUtc;
        protectedSecret.UpdatedAtUtc = now;

        await _mongoOperations.SaveAsync(protectedSecret, cancellationToken);
    }

    /// <summary>
    /// Gets and decrypts one shared secret by identifier.
    /// </summary>
    public async Task<string> GetSecretAsync(string secretId, CancellationToken cancellationToken = default)
    {
        string normalizedSecretId = NormalizeSecretId(secretId);
        if (normalizedSecretId == null)
            return null;

        SharedSecret storedSecret = await _mongoOperations.GetByIdAsync(normalizedSecretId, cancellationToken);
        return storedSecret == null ? null : SharedSecretProtector.Unprotect(storedSecret);
    }

    /// <summary>
    /// Deletes one shared secret by identifier.
    /// </summary>
    public async Task<bool> DeleteSecretAsync(string secretId, CancellationToken cancellationToken = default)
    {
        string normalizedSecretId = NormalizeSecretId(secretId);
        if (normalizedSecretId == null)
            return false;

        return (await _mongoOperations.DeleteAsync(normalizedSecretId, cancellationToken)).DeletedCount == 1;
    }

    internal static string NormalizeSecretId(string secretId)
    {
        if (string.IsNullOrWhiteSpace(secretId))
            return null;

        return secretId.Trim().ToLowerInvariant();
    }
}