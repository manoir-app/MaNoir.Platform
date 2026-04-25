using Home.Common.Messages;
using System;
using System.Security.Cryptography;
using System.Text;

namespace MaNoir.Core.Secrets;

internal static class SharedSecretExchangeProtector
{
    private const int SessionKeySizeInBytes = 32;
    private const int NonceSizeInBytes = 12;
    private const int AuthenticationTagSizeInBytes = 16;
    private const string EncryptionMode = "rsa-oaep-sha256+aes-256-gcm/v1";

    public static ContributionEncryptedSecretPayload ProtectForPublicKey(string clearText, string publicKeyPem)
    {
        if (clearText == null)
            throw new ArgumentNullException(nameof(clearText));

        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("The public key PEM cannot be empty.", nameof(publicKeyPem));

        byte[] sessionKey = RandomNumberGenerator.GetBytes(SessionKeySizeInBytes);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
        byte[] plainBytes = Encoding.UTF8.GetBytes(clearText);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] authenticationTag = new byte[AuthenticationTagSizeInBytes];

        using AesGcm aes = new AesGcm(sessionKey, AuthenticationTagSizeInBytes);
        aes.Encrypt(nonce, plainBytes, cipherBytes, authenticationTag);

        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        byte[] encryptedSessionKey = rsa.Encrypt(sessionKey, RSAEncryptionPadding.OaepSHA256);

        return new ContributionEncryptedSecretPayload()
        {
            EncryptionMode = EncryptionMode,
            EncryptedKey = Convert.ToBase64String(encryptedSessionKey),
            EncryptedData = Convert.ToBase64String(cipherBytes),
            Nonce = Convert.ToBase64String(nonce),
            AuthenticationTag = Convert.ToBase64String(authenticationTag)
        };
    }
}