using MaNoir.Core.Contracts.Models.Users;
using System;
using System.Text;

namespace MaNoir.Core.Users;

public sealed partial class UserLogic
{
    /// <summary>
    /// Computes the canonical identifier for a guest user.
    /// </summary>
    /// <param name="user">The candidate guest user.</param>
    /// <returns>
    /// A normalized lower-case identifier built from the existing identifier,
    /// the guest name, the email address, or a generated fallback value.
    /// </returns>
    public static string GetGuestUserId(User user)
    {
        if (user == null)
            return null;

        string userId = user.Id;

        if (string.IsNullOrWhiteSpace(userId))
            userId = SanitizeGuestName(user.Name, user.FirstName);

        if (string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(user.MainEmail))
            userId = SanitizeGuestName(user.MainEmail);

        if (string.IsNullOrWhiteSpace(userId))
            userId = Guid.NewGuid().ToString("N");

        return userId.ToLowerInvariant();
    }

    /// <summary>
    /// Builds a guest identifier fragment from the provided name parts.
    /// </summary>
    /// <param name="name">The main name part.</param>
    /// <param name="firstName">The optional secondary name part.</param>
    /// <returns>An ASCII-only concatenation suitable for guest identifiers.</returns>
    public static string SanitizeGuestName(string name, string firstName = null)
    {
        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(name))
            builder.Append(Sanitize(name));

        if (!string.IsNullOrWhiteSpace(firstName))
            builder.Append(Sanitize(firstName));

        return builder.ToString();
    }

    /// <summary>
    /// Removes any character that is not an ASCII letter or digit.
    /// </summary>
    /// <param name="value">The source value to sanitize.</param>
    /// <returns>The sanitized value, or an empty string when no character is kept.</returns>
    public static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        foreach (char character in value)
        {
            if (character >= '0' && character <= '9')
                builder.Append(character);

            if (character >= 'a' && character <= 'z')
                builder.Append(character);

            if (character >= 'A' && character <= 'Z')
                builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Copies the editable guest profile fields from a source user into a target user.
    /// </summary>
    /// <param name="target">The existing guest user to update.</param>
    /// <param name="source">The incoming guest payload.</param>
    public static void ApplyGuestProfileUpdate(User target, User source)
    {
        if (target == null || source == null)
            return;

        target.CommonName = source.CommonName;
        target.FirstName = source.FirstName;
        target.MainEmail = source.MainEmail;
        target.MainPhoneNumber = source.MainPhoneNumber;
        target.Name = source.Name;
    }

    /// <summary>
    /// Initializes the domain state required for a newly created guest user.
    /// </summary>
    /// <param name="user">The guest user to initialize.</param>
    /// <param name="now">The current business time.</param>
    public static void InitializeGuestUser(User user, DateTimeOffset now)
    {
        if (user == null)
            return;

        user.IsGuest = true;
        user.IsMain = false;
        user.DeleteAfter = now.AddDays(1);
        user.Id = GetGuestUserId(user);
    }
}