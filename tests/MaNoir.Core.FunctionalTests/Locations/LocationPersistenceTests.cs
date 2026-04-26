using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Locations;

[TestClass]
[DoNotParallelize]
public sealed class LocationPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task UpsertAsync_ShouldPersistLocationWithGeneratedZoneAndRoomIdentifiers()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        LocationLogic logic = new LocationLogic();

        Location storedLocation = await logic.UpsertAsync(new Location()
        {
            Name = "Home",
            City = "Paris",
            Zones =
            {
                new LocationZone()
                {
                    Name = "Ground floor",
                    Rooms =
                    {
                        new LocationRoom()
                        {
                            Name = "Living room",
                            FloorLevel = 0
                        }
                    }
                }
            }
        });

        Location reloadedLocation = await logic.GetByIdAsync(storedLocation.Id);

        Assert.IsNotNull(storedLocation);
        Assert.IsNotNull(reloadedLocation);
        Assert.AreEqual("Home", reloadedLocation.Name);
        Assert.AreEqual("Paris", reloadedLocation.City);
        Assert.IsFalse(string.IsNullOrWhiteSpace(reloadedLocation.Id));
        Assert.AreEqual(1, reloadedLocation.Zones.Count);
        Assert.IsFalse(string.IsNullOrWhiteSpace(reloadedLocation.Zones[0].Id));
        Assert.AreEqual(1, reloadedLocation.Zones[0].Rooms.Count);
        Assert.IsFalse(string.IsNullOrWhiteSpace(reloadedLocation.Zones[0].Rooms[0].Id));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task DeleteAsync_ShouldRemovePersistedLocationAndReflectInListing()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        LocationLogic logic = new LocationLogic();

        Location createdLocation = await logic.UpsertAsync(new Location()
        {
            Name = "Work",
            City = "Lille"
        });

        Location updatedLocation = await logic.UpsertAsync(new Location()
        {
            Id = createdLocation.Id.ToUpperInvariant(),
            Name = "Work updated",
            City = "Lyon"
        });

        System.Collections.Generic.List<Location> locationsBeforeDelete = await logic.GetAllAsync();
        bool deleted = await logic.DeleteAsync(createdLocation.Id.ToUpperInvariant());
        Location deletedLocation = await logic.GetByIdAsync(createdLocation.Id);
        System.Collections.Generic.List<Location> locationsAfterDelete = await logic.GetAllAsync();

        Assert.IsNotNull(createdLocation);
        Assert.IsNotNull(updatedLocation);
        Assert.AreEqual(createdLocation.Id, updatedLocation.Id);
        Assert.AreEqual("Work updated", updatedLocation.Name);
        Assert.AreEqual("Lyon", updatedLocation.City);
        Assert.AreEqual(1, locationsBeforeDelete.Count);
        Assert.IsTrue(deleted);
        Assert.IsNull(deletedLocation);
        Assert.AreEqual(0, locationsAfterDelete.Count);
    }
}