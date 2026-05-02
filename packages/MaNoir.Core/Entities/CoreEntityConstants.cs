namespace MaNoir.Core.Entities;

/// <summary>
/// Provides entity constants owned by the Core package.
/// </summary>
public static class CoreEntityConstants
{
    /// <summary>
    /// Built-in entity categories published by Core.
    /// </summary>
    public static class Categories
    {
        /// <summary>
        /// Default uncategorized entity bucket.
        /// </summary>
        public const string Default = "";

        /// <summary>
        /// Category used for diagnostic entities.
        /// </summary>
        public const string Diagnostic = "diag";

        /// <summary>
        /// Category used for configuration entities.
        /// </summary>
        public const string Configuration = "config";
    }

    /// <summary>
    /// Built-in entity kinds published by Core.
    /// </summary>
    public static class Kinds
    {
        /// <summary>
        /// Generic Core status entity kind.
        /// </summary>
        public const string Status = "core:status";

        /// <summary>
        /// Astronomical sun entity kind.
        /// </summary>
        public const string Sun = "manoirapp:sun";

        /// <summary>
        /// Weather entity kind.
        /// </summary>
        public const string Weather = "manoirapp:weather";
    }
}