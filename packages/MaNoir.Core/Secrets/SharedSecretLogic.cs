using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Secrets;

/// <summary>
/// Implements local shared secret persistence protected by environment-backed encryption material.
/// </summary>
/// <remarks>
/// <para>
/// The encryption key is derived at runtime from the <c>HOMEAUTOMATION_APIKEY</c> and
/// <c>HOMEAUTOMATION_SECRETS_SALT</c> environment variables. The salt must contain a base64 payload
/// with at least 16 bytes of random data.
/// </para>
/// <para>Example:</para>
/// <code>
/// Environment.SetEnvironmentVariable("HOMEAUTOMATION_APIKEY", "a-stable-runtime-secret");
/// Environment.SetEnvironmentVariable("HOMEAUTOMATION_SECRETS_SALT", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
///
/// SharedSecretLogic logic = new SharedSecretLogic();
/// await logic.SetSecretAsync("mqtt.password", "P@ssword-42");
/// string mqttPassword = await logic.GetSecretAsync("mqtt.password");
/// </code>
/// <para>
/// Contribution settings may then reference the stored value with the <c>{{ SECRET:mqtt.password }}</c>
/// syntax and resolve it through the contribution secret exchange flow.
/// </para>
/// </remarks>
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
    /// <remarks>
    /// <para>Example:</para>
    /// <code>
    /// SharedSecretLogic logic = new SharedSecretLogic();
    /// await logic.SetSecretAsync("weather.api-key", "secret-value", cancellationToken);
    /// </code>
    /// <para>
    /// The secret identifier is normalized to lower-case before storage, so <c>Weather.Api-Key</c>
    /// and <c>weather.api-key</c> target the same stored secret.
    /// </para>
    /// </remarks>
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
    /// <remarks>
    /// <para>Example:</para>
    /// <code>
    /// SharedSecretLogic logic = new SharedSecretLogic();
    /// string apiKey = await logic.GetSecretAsync("weather.api-key", cancellationToken);
///
    /// if (apiKey == null)
    /// {
    ///     // Secret not configured yet.
    /// }
    /// </code>
    /// <para>
    /// This method returns <see langword="null"/> when the identifier is empty or when the secret does
    /// not exist. It throws when the runtime encryption material is missing or no longer matches the
    /// values used when the secret was stored.
    /// </para>
    /// </remarks>
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
    /// <remarks>
    /// <para>Example:</para>
    /// <code>
    /// SharedSecretLogic logic = new SharedSecretLogic();
    /// bool deleted = await logic.DeleteSecretAsync("weather.api-key", cancellationToken);
    /// </code>
    /// <para>
    /// The method returns <see langword="false"/> when the identifier is empty or when no stored secret
    /// matches the normalized identifier.
    /// </para>
    /// </remarks>
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