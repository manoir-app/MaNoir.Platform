namespace MaNoir.Core.Locations;

/// <summary>
/// Provides entity constants owned by the Locations domain.
/// </summary>
public static class LocationEntityConstants
{
    public static class Kinds
    {
        public const string Location = "manoirapp:location";
        public const string Room = "manoirapp:location/room";
    }

    public static class Categories
    {
        public const string Address = "address";
        public const string Structure = "structure";
        public const string Properties = "properties";
    }
}