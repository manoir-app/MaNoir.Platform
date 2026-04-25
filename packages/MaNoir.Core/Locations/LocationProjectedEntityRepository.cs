using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Locations;

/// <summary>
/// Projects locations and rooms as read-only entities.
/// </summary>
public sealed class LocationProjectedEntityRepository : IProjectedEntityRepository
{
    /// <inheritdoc/>
    public string Source => "locations/catalog";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> SupportedKinds =>
    [
        LocationEntityConstants.Kinds.Location,
        LocationEntityConstants.Kinds.Room
    ];

    /// <inheritdoc/>
    public async Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default)
    {
        string normalizedKind = EntityLogic.NormalizeEntityKind(kind);
        string normalizedEntityId = EntityLogic.NormalizeEntityId(entityId);
        if (normalizedKind == null || normalizedEntityId == null)
            return null;

        LocationLogic locationLogic = new LocationLogic();
        if (normalizedKind == LocationEntityConstants.Kinds.Location)
        {
            Location location = await locationLogic.GetByIdAsync(normalizedEntityId, cancellationToken);
            return ToLocationEntity(location);
        }

        if (normalizedKind != LocationEntityConstants.Kinds.Room)
            return null;

        List<Location> locations = await locationLogic.GetAllAsync(cancellationToken);
        foreach (Location location in locations)
        {
            RoomProjection roomProjection = FindRoom(location, normalizedEntityId);
            if (roomProjection != null)
                return ToRoomEntity(location, roomProjection.Zone, roomProjection.Room);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<List<Entity>> GetByKindsAsync(IReadOnlyCollection<string> kinds, CancellationToken cancellationToken = default)
    {
        List<string> normalizedKinds = EntityLogic.NormalizeEntityKinds(kinds);
        if (normalizedKinds.Count == 0)
            return [];

        bool includeLocations = normalizedKinds.Contains(LocationEntityConstants.Kinds.Location);
        bool includeRooms = normalizedKinds.Contains(LocationEntityConstants.Kinds.Room);
        if (!includeLocations && !includeRooms)
            return [];

        LocationLogic locationLogic = new LocationLogic();
        List<Location> locations = await locationLogic.GetAllAsync(cancellationToken);
        List<Entity> entities = [];

        foreach (Location location in locations)
        {
            if (includeLocations)
            {
                Entity locationEntity = ToLocationEntity(location);
                if (locationEntity != null)
                    entities.Add(locationEntity);
            }

            if (!includeRooms || location?.Zones == null)
                continue;

            foreach (LocationZone zone in location.Zones)
            {
                if (zone?.Rooms == null)
                    continue;

                foreach (LocationRoom room in zone.Rooms)
                {
                    Entity roomEntity = ToRoomEntity(location, zone, room);
                    if (roomEntity != null)
                        entities.Add(roomEntity);
                }
            }
        }

        return entities;
    }

    private static Entity ToLocationEntity(Location location)
    {
        string locationId = LocationLogic.NormalizeLocationId(location?.Id);
        if (locationId == null)
            return null;

        Entity entity = new Entity()
        {
            Id = locationId,
            EntityKind = LocationEntityConstants.Kinds.Location,
            Name = location.Name,
            LocationId = locationId,
            Datas =
            {
                ["LocationKind"] = CreateData(location.Kind.ToString(), LocationEntityConstants.Categories.Structure),
                ["HasAutomationsMesh"] = CreateData(location.HasAutomationsMesh ? "true" : "false", LocationEntityConstants.Categories.Structure),
                ["ZoneCount"] = CreateData(location.Zones?.Count ?? 0, LocationEntityConstants.Categories.Structure),
                ["RoomCount"] = CreateData(CountRooms(location), LocationEntityConstants.Categories.Structure)
            }
        };

        AddIfPresent(entity, "StreetAddress", location.StreetAddress, LocationEntityConstants.Categories.Address);
        AddIfPresent(entity, "ZipCode", location.ZipCode, LocationEntityConstants.Categories.Address);
        AddIfPresent(entity, "City", location.City, LocationEntityConstants.Categories.Address);
        AddIfPresent(entity, "State", location.State, LocationEntityConstants.Categories.Address);
        AddIfPresent(entity, "Country", location.Country, LocationEntityConstants.Categories.Address);

        if (location.Coordinates != null)
        {
            entity.Datas["Latitude"] = CreateData(location.Coordinates.Latitude, LocationEntityConstants.Categories.Address);
            entity.Datas["Longitude"] = CreateData(location.Coordinates.Longitude, LocationEntityConstants.Categories.Address);
        }

        return entity;
    }

    private static Entity ToRoomEntity(Location location, LocationZone zone, LocationRoom room)
    {
        string roomId = EntityLogic.NormalizeEntityId(room?.Id);
        string locationId = LocationLogic.NormalizeLocationId(location?.Id);
        if (roomId == null || locationId == null)
            return null;

        Entity entity = new Entity()
        {
            Id = roomId,
            EntityKind = LocationEntityConstants.Kinds.Room,
            Name = room.Name,
            LocationId = locationId,
            Datas =
            {
                ["LocationId"] = CreateData(locationId, LocationEntityConstants.Categories.Structure),
                ["LocationName"] = CreateData(location.Name, LocationEntityConstants.Categories.Structure),
                ["ZoneId"] = CreateData(zone?.Id, LocationEntityConstants.Categories.Structure),
                ["ZoneName"] = CreateData(zone?.Name, LocationEntityConstants.Categories.Structure),
                ["RoomKind"] = CreateData(room.RoomKind.ToString(), LocationEntityConstants.Categories.Structure),
                ["FloorLevel"] = CreateData(room.FloorLevel, LocationEntityConstants.Categories.Structure)
            }
        };

        if (room.Properties != null)
        {
            if (room.Properties.Temperature.HasValue)
                entity.Datas["Temperature"] = CreateData(room.Properties.Temperature.Value, LocationEntityConstants.Categories.Properties);

            if (room.Properties.Humidity.HasValue)
                entity.Datas["Humidity"] = CreateData(room.Properties.Humidity.Value, LocationEntityConstants.Categories.Properties);

            if (room.Properties.Pressure.HasValue)
                entity.Datas["Pressure"] = CreateData(room.Properties.Pressure.Value, LocationEntityConstants.Categories.Properties);

            if (room.Properties.Occupancy.HasValue)
                entity.Datas["Occupancy"] = CreateData(room.Properties.Occupancy.Value.ToString(), LocationEntityConstants.Categories.Properties);
        }

        return entity;
    }

    private static int CountRooms(Location location)
    {
        if (location?.Zones == null)
            return 0;

        return location.Zones.Sum(zone => zone?.Rooms?.Count ?? 0);
    }

    private static void AddIfPresent(Entity entity, string key, string value, string category)
    {
        if (entity == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            return;

        entity.Datas[key] = CreateData(value, category);
    }

    private static EntityData CreateData(string value, string category)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new EntityData()
        {
            SimpleType = "System.String",
            SimpleValue = value,
            Category = category
        };
    }

    private static EntityData CreateData(int value, string category)
    {
        return new EntityData()
        {
            SimpleType = "System.Int64",
            IntSimpleValue = value,
            Category = category
        };
    }

    private static EntityData CreateData(decimal value, string category)
    {
        return new EntityData()
        {
            SimpleType = "System.Decimal",
            DecimalSimpleValue = value,
            Category = category
        };
    }

    private static RoomProjection FindRoom(Location location, string normalizedRoomId)
    {
        if (location?.Zones == null)
            return null;

        foreach (LocationZone zone in location.Zones)
        {
            if (zone?.Rooms == null)
                continue;

            foreach (LocationRoom room in zone.Rooms)
            {
                if (EntityLogic.NormalizeEntityId(room?.Id) != normalizedRoomId)
                    continue;

                return new RoomProjection(zone, room);
            }
        }

        return null;
    }

    private sealed class RoomProjection
    {
        public RoomProjection(LocationZone zone, LocationRoom room)
        {
            Zone = zone;
            Room = room;
        }

        public LocationZone Zone { get; }

        public LocationRoom Room { get; }
    }
}