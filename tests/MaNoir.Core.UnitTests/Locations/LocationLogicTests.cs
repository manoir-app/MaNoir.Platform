using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MaNoir.Core.UnitTests.Locations;

[TestClass]
public sealed class LocationLogicTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void PrepareForSave_ShouldAssignNormalizedLocationAndNestedIdentifiers()
    {
        Location location = new Location()
        {
            Id = "A0B1C2D3-E4F5-6789-ABCD-EF0123456789",
            Name = "Home",
            Zones =
            {
                new LocationZone()
                {
                    Name = "Ground floor",
                    Rooms =
                    {
                        new LocationRoom() { Name = "Living room" }
                    }
                }
            }
        };

        LocationLogic.PrepareForSave(location);

        Assert.AreEqual("a0b1c2d3-e4f5-6789-abcd-ef0123456789", location.Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(location.Zones[0].Id));
        Assert.AreEqual(location.Zones[0].Id.ToUpperInvariant(), location.Zones[0].Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(location.Zones[0].Rooms[0].Id));
        Assert.AreEqual(location.Zones[0].Rooms[0].Id.ToUpperInvariant(), location.Zones[0].Rooms[0].Id);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void NormalizeLocationId_ShouldReturnLowerCaseIdentifier()
    {
        string normalizedLocationId = LocationLogic.NormalizeLocationId("A0B1C2D3-E4F5-6789-ABCD-EF0123456789");

        Assert.AreEqual("a0b1c2d3-e4f5-6789-abcd-ef0123456789", normalizedLocationId);
    }
}