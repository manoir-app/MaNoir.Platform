namespace MaNoir.Core.Users;

/// <summary>
/// Provides entity constants owned by the Users domain.
/// </summary>
public static class UserEntityConstants
{
    /// <summary>
    /// Entity kinds used by the Users domain.
    /// </summary>
    public static class Kinds
    {
        /// <summary>
        /// User entity kind.
        /// </summary>
        public const string User = "manoirapp:user";
    }

    /// <summary>
    /// Entity categories used by the Users domain.
    /// </summary>
    public static class Categories
    {
        /// <summary>
        /// Identity-related user data.
        /// </summary>
        public const string Identity = "identity";

        /// <summary>
        /// User flag data such as admin or guest markers.
        /// </summary>
        public const string Flags = "flags";
    }
}