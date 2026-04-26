namespace MaNoir.Core.Entities;

/// <summary>
/// Provides entity constants owned by the Core package.
/// </summary>
public static class CoreEntityConstants
{
    public static class Categories
    {
        public const string Default = "";
        public const string Diagnostic = "diag";
        public const string Configuration = "config";
    }

    public static class Kinds
    {
        public const string Status = "core:status";
        public const string Sun = "manoirapp:sun";
        public const string Weather = "manoirapp:weather";
    }
}