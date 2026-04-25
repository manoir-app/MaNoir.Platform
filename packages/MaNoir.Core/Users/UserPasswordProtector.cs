using System;
using System.Security.Cryptography;

namespace MaNoir.Core.Users;

/// <summary>
/// Provides password hashing and verification for user authentication.
/// </summary>
public static class UserPasswordProtector
{
    private const string FormatPrefix = "manoir-pbkdf2-sha256-v1";
    private const int SaltSizeInBytes = 16;
    private const int KeySizeInBytes = 32;
    private const int IterationCount = 100000;

    /// <summary>
    /// Hashes one clear text password using the current supported format.
    /// </summary>
    public static string HashPassword(string clearTextPassword)
    {
        if (string.IsNullOrWhiteSpace(clearTextPassword))
            throw new ArgumentException("The password cannot be empty.", nameof(clearTextPassword));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(clearTextPassword, salt, IterationCount, HashAlgorithmName.SHA256, KeySizeInBytes);
        return string.Concat(
            FormatPrefix,
            '$',
            IterationCount,
            '$',
            Convert.ToBase64String(salt),
            '$',
            Convert.ToBase64String(hash));
    }

    /// <summary>
    /// Verifies a clear text password against the stored hash.
    /// </summary>
    public static bool VerifyPassword(string clearTextPassword, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(clearTextPassword) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        string[] parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], FormatPrefix, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(parts[1], out int iterationCount) || iterationCount <= 0)
            return false;

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(clearTextPassword, salt, iterationCount, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}