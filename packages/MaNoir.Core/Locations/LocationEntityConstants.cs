namespace MaNoir.Core.Locations;

/// <summary>
/// Provides entity constants owned by the Locations domain.
/// </summary>
public static class LocationEntityConstants
{
    /// <summary>
    /// Entity kinds used by the Locations domain.
    /// </summary>
    public static class Kinds
    {
        /// <summary>
        /// Location aggregate entity kind.
        /// </summary>
        public const string Location = "manoirapp:location";

        /// <summary>
        /// Room entity kind nested under a location.
        /// </summary>
        public const string Room = "manoirapp:location/room";
    }

    /// <summary>
    /// Entity categories used by the Locations domain.
    /// </summary>
    public static class Categories
    {
        /// <summary>
        /// Address-oriented location data.
        /// </summary>
        public const string Address = "address";

        /// <summary>
        /// Structural location data such as zones and rooms.
        /// </summary>
        public const string Structure = "structure";

        /// <summary>
        /// Miscellaneous location properties.
        /// </summary>
        public const string Properties = "properties";
    }
}