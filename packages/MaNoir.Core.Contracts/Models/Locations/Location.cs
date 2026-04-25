using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Locations;

/// <summary>
/// Identifies the high-level kind of a location.
/// </summary>
public enum LocationKind
{
    Home = 0,
    Work = 1,
    Family = 2,
    Friends = 3
}

/// <summary>
/// Represents a pair of geographic coordinates.
/// </summary>
public sealed class GeoCoordinates
{
    /// <summary>
    /// Gets or sets the longitude.
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Gets or sets the latitude.
    /// </summary>
    public decimal Latitude { get; set; }
}

/// <summary>
/// Identifies the current occupancy state of a location element.
/// </summary>
public enum OccupancyState
{
    ActivePresence,
    RecentPresent,
    NoPresence
}

/// <summary>
/// Identifies the aggregation rule kind.
/// </summary>
public enum MeasureAggregationRuleKind
{
    Average,
    Min,
    Max
}

/// <summary>
/// Represents a source participating in a measure aggregation rule.
/// </summary>
public sealed class MeasureAggregationRuleSource
{
    /// <summary>
    /// Gets or sets the source type.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the source identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the source weight.
    /// </summary>
    public decimal Weight { get; set; } = 1M;
}

/// <summary>
/// Represents a rule used to aggregate measures.
/// </summary>
public sealed class MeasureAggregationRule
{
    public MeasureAggregationRule()
    {
        Sources = [];
    }

    /// <summary>
    /// Gets or sets the aggregation sources.
    /// </summary>
    public List<MeasureAggregationRuleSource> Sources { get; set; }

    /// <summary>
    /// Gets or sets the aggregation kind.
    /// </summary>
    public MeasureAggregationRuleKind Kind { get; set; } = MeasureAggregationRuleKind.Average;
}

/// <summary>
/// Represents a stored location.
/// </summary>
public sealed class Location
{
    public Location()
    {
        Zones = [];
        MeasureAggregationRules = [];
    }

    /// <summary>
    /// Gets or sets the location identifier.
    /// </summary>
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the location display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the location hosts an automation mesh.
    /// </summary>
    public bool HasAutomationsMesh { get; set; }

    /// <summary>
    /// Gets or sets the location kind.
    /// </summary>
    public LocationKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the coordinates.
    /// </summary>
    public GeoCoordinates Coordinates { get; set; }

    /// <summary>
    /// Gets or sets the street address.
    /// </summary>
    public string StreetAddress { get; set; }

    /// <summary>
    /// Gets or sets the zip code.
    /// </summary>
    public string ZipCode { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; }

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    public string State { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// Gets or sets the zones.
    /// </summary>
    public List<LocationZone> Zones { get; set; }

    /// <summary>
    /// Gets or sets the location properties.
    /// </summary>
    public LocationElementProperties Properties { get; set; }

    /// <summary>
    /// Gets or sets the measure aggregation rules.
    /// </summary>
    public List<MeasureAggregationRule> MeasureAggregationRules { get; set; }
}

/// <summary>
/// Represents a zone inside a location.
/// </summary>
public sealed class LocationZone
{
    public LocationZone()
    {
        Rooms = [];
        MeasureAggregationRules = [];
    }

    /// <summary>
    /// Gets or sets the zone identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the zone display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the rooms contained in the zone.
    /// </summary>
    public List<LocationRoom> Rooms { get; set; }

    /// <summary>
    /// Gets or sets the zone properties.
    /// </summary>
    public LocationElementProperties Properties { get; set; }

    /// <summary>
    /// Gets or sets the measure aggregation rules.
    /// </summary>
    public List<MeasureAggregationRule> MeasureAggregationRules { get; set; }
}

/// <summary>
/// Identifies the kind of a room.
/// </summary>
public enum RoomKind
{
    Generic,
    Corridor,
    Bedroom,
    Bathroom,
    Kitchen,
    LivingRoom,
    DiningRoom,
    Office,
    ReadingRoom,
    Pool
}

/// <summary>
/// Represents a room inside a zone.
/// </summary>
public sealed class LocationRoom
{
    public LocationRoom()
    {
        RoomMappingForServices = [];
        GroupMappingForServices = [];
        Shape = [];
        Walls = [];
        Properties = new LocationElementProperties();
        MeasureAggregationRules = [];
    }

    /// <summary>
    /// Gets or sets the room identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the room display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the room mappings per service.
    /// </summary>
    public Dictionary<string, List<string>> RoomMappingForServices { get; set; }

    /// <summary>
    /// Gets or sets the group mappings per service.
    /// </summary>
    public Dictionary<string, List<string>> GroupMappingForServices { get; set; }

    /// <summary>
    /// Gets or sets the room kind.
    /// </summary>
    public RoomKind RoomKind { get; set; }

    /// <summary>
    /// Gets or sets the floor level.
    /// </summary>
    public int FloorLevel { get; set; }

    /// <summary>
    /// Gets or sets the room properties.
    /// </summary>
    public LocationElementProperties Properties { get; set; }

    /// <summary>
    /// Gets or sets the room shape.
    /// </summary>
    public List<LocationPoint> Shape { get; set; }

    /// <summary>
    /// Gets or sets the room walls.
    /// </summary>
    public List<LocationWall> Walls { get; set; }

    /// <summary>
    /// Gets or sets the measure aggregation rules.
    /// </summary>
    public List<MeasureAggregationRule> MeasureAggregationRules { get; set; }
}

/// <summary>
/// Represents common properties of a location element.
/// </summary>
public sealed class LocationElementProperties
{
    /// <summary>
    /// Gets or sets the temperature.
    /// </summary>
    public decimal? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the humidity.
    /// </summary>
    public decimal? Humidity { get; set; }

    /// <summary>
    /// Gets or sets the pressure.
    /// </summary>
    public decimal? Pressure { get; set; }

    /// <summary>
    /// Gets or sets the occupancy state.
    /// </summary>
    public OccupancyState? Occupancy { get; set; }

    /// <summary>
    /// Gets or sets the additional properties.
    /// </summary>
    public Dictionary<string, string> MoreProperties { get; set; }
}

/// <summary>
/// Represents a point in a location shape.
/// </summary>
public sealed class LocationPoint
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public int Y { get; set; }
}

/// <summary>
/// Represents a wall in a location shape.
/// </summary>
public sealed class LocationWall
{
    public LocationWall()
    {
        Points = [];
        Thickness = 1;
    }

    /// <summary>
    /// Gets or sets the wall points.
    /// </summary>
    public List<LocationPoint> Points { get; set; }

    /// <summary>
    /// Gets or sets the wall thickness.
    /// </summary>
    public int Thickness { get; set; }
}