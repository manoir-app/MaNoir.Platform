using MaNoir.Core.Contracts.Models.Locations;
using System;

namespace MaNoir.Core.Locations;

/// <summary>
/// Provides business logic for locations.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// Location location = new Location() { Label = "Maison" };
/// LocationLogic.PrepareForSave(location);
/// </code>
/// <para>
/// The location helpers normalize aggregate identifiers and ensure nested zones and rooms are ready for persistence.
/// </para>
/// </remarks>
public sealed partial class LocationLogic
{
    private readonly LocationMongoOperations _mongoOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationLogic"/> class.
    /// </summary>
    public LocationLogic()
    {
        _mongoOperations = new LocationMongoOperations();
    }

    /// <summary>
    /// Normalizes a location identifier.
    /// </summary>
    /// <remarks>
    /// <para>This helper lower-cases persisted identifiers so that API and storage use a stable canonical form.</para>
    /// </remarks>
    public static string NormalizeLocationId(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
            return null;

        return locationId.ToLowerInvariant();
    }

    /// <summary>
    /// Ensures that a location and its nested zones and rooms have identifiers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this before saving imported or UI-authored location trees to avoid missing identifiers on zones and rooms.
    /// </para>
    /// </remarks>
    public static void EnsureNestedIdentifiers(Location location)
    {
        if (location == null || location.Zones == null)
            return;

        foreach (LocationZone zone in location.Zones)
        {
            if (zone == null)
                continue;

            if (string.IsNullOrWhiteSpace(zone.Id))
                zone.Id = Guid.NewGuid().ToString("D").ToUpperInvariant();
            else
                zone.Id = zone.Id.ToUpperInvariant();

            if (zone.Rooms == null)
                continue;

            foreach (LocationRoom room in zone.Rooms)
            {
                if (room == null)
                    continue;

                if (string.IsNullOrWhiteSpace(room.Id))
                    room.Id = Guid.NewGuid().ToString("D").ToUpperInvariant();
                else
                    room.Id = room.Id.ToUpperInvariant();
            }
        }
    }

    /// <summary>
    /// Prepares a location aggregate for persistence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method combines root identifier normalization and nested identifier generation, making it the recommended pre-save step.
    /// </para>
    /// </remarks>
    public static void PrepareForSave(Location location)
    {
        if (location == null)
            return;

        if (string.IsNullOrWhiteSpace(location.Id))
            location.Id = Guid.NewGuid().ToString("D").ToLowerInvariant();
        else
            location.Id = NormalizeLocationId(location.Id);

        EnsureNestedIdentifiers(location);
    }
}