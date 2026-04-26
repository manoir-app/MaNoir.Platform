using System;
using System.Security.Cryptography;
using System.Text;

namespace MaNoir.Core.Secrets;

internal static class SharedSecretProtector
{
    private const string ApiKeyEnvironmentVariableName = "HOMEAUTOMATION_APIKEY";
    private const string SaltEnvironmentVariableName = "HOMEAUTOMATION_SECRETS_SALT";
    private const int KeySizeInBytes = 32;
    private const int NonceSizeInBytes = 12;
    private const int AuthenticationTagSizeInBytes = 16;
    private const int Pbkdf2Iterations = 100000;

    public static SharedSecret Protect(string clearText)
    {
        if (clearText == null)
            throw new ArgumentNullException(nameof(clearText));

        byte[] key = DeriveEncryptionKey();
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
        byte[] plainBytes = Encoding.UTF8.GetBytes(clearText);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] authenticationTag = new byte[AuthenticationTagSizeInBytes];

        using AesGcm aes = new AesGcm(key, AuthenticationTagSizeInBytes);
        aes.Encrypt(nonce, plainBytes, cipherBytes, authenticationTag);

        return new SharedSecret()
        {
            EncryptedData = Convert.ToBase64String(cipherBytes),
            Nonce = Convert.ToBase64String(nonce),
            AuthenticationTag = Convert.ToBase64String(authenticationTag),
            EncryptionMode = SharedSecret.EncryptionModeAes256GcmPbkdf2Sha256V1
        };
    }

    public static string Unprotect(SharedSecret secret)
    {
        if (secret == null)
            throw new ArgumentNullException(nameof(secret));

        if (!string.Equals(secret.EncryptionMode, SharedSecret.EncryptionModeAes256GcmPbkdf2Sha256V1, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported shared secret encryption mode '{secret.EncryptionMode}'.");

        byte[] key = DeriveEncryptionKey();
        byte[] nonce = Convert.FromBase64String(secret.Nonce ?? string.Empty);
        byte[] cipherBytes = Convert.FromBase64String(secret.EncryptedData ?? string.Empty);
        byte[] authenticationTag = Convert.FromBase64String(secret.AuthenticationTag ?? string.Empty);
        byte[] plainBytes = new byte[cipherBytes.Length];

        using AesGcm aes = new AesGcm(key, AuthenticationTagSizeInBytes);
        aes.Decrypt(nonce, cipherBytes, authenticationTag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveEncryptionKey()
    {
        string apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Shared secret protection requires the '{ApiKeyEnvironmentVariableName}' environment variable.");

        string saltText = Environment.GetEnvironmentVariable(SaltEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(saltText))
            throw new InvalidOperationException($"Shared secret protection requires the '{SaltEnvironmentVariableName}' environment variable.");

        byte[] salt;
        try
        {
            salt = Convert.FromBase64String(saltText);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException($"The '{SaltEnvironmentVariableName}' environment variable must contain a valid base64 payload.", exception);
        }

        if (salt.Length < 16)
            throw new InvalidOperationException($"The '{SaltEnvironmentVariableName}' environment variable must contain at least 16 bytes of random data.");

        return Rfc2898DeriveBytes.Pbkdf2(apiKey, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySizeInBytes);
    }
}