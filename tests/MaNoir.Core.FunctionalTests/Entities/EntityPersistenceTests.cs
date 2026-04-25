using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Entities;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Locations;
using MaNoir.Core.Mesh;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Entities;

[TestClass]
[DoNotParallelize]
public sealed class EntityPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task UpsertAsync_ShouldPersistNativeEntityAndKeepItWritable()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        EntityLogic logic = new EntityLogic();

        Entity storedEntity = await logic.UpsertAsync(new Entity()
        {
            Id = "NATIVE-ENTITY",
            EntityKind = CoreEntityConstants.Kinds.Status,
            Name = "Status",
            Datas =
            {
                ["Mode"] = new EntityData() { SimpleType = "System.String", SimpleValue = "Home", Category = CoreEntityConstants.Categories.Configuration }
            }
        });

        Entity reloadedEntity = await logic.GetByIdAsync(CoreEntityConstants.Kinds.Status, "native-entity");

        Assert.IsNotNull(storedEntity);
        Assert.IsNotNull(reloadedEntity);
        Assert.AreEqual("native-entity", storedEntity.Id);
        Assert.AreEqual(CoreEntityConstants.Kinds.Status, storedEntity.EntityKind);
        Assert.AreEqual("Status", reloadedEntity.Name);
        Assert.IsFalse(reloadedEntity.IsReadOnly);
        Assert.IsNull(reloadedEntity.Source);
        Assert.AreEqual("Home", reloadedEntity.Datas["Mode"].SimpleValue);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetByKindsAsync_ShouldMergeNativeAndProjectedEntities()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        EntityProjectionRepositoryRegistry registry = new EntityProjectionRepositoryRegistry();
        registry.Register(new FakeProjectedEntityRepository());

        EntityLogic logic = new EntityLogic(registry);
        await logic.UpsertAsync(new Entity()
        {
            Id = "native-01",
            EntityKind = CoreEntityConstants.Kinds.Status,
            Name = "Native"
        });

        List<Entity> entities = await logic.GetByKindsAsync([CoreEntityConstants.Kinds.Status, "demo:projection"]);

        Assert.AreEqual(2, entities.Count);
        Assert.IsTrue(entities.Any(entity => entity.EntityKind == CoreEntityConstants.Kinds.Status && entity.Id == "native-01" && !entity.IsReadOnly));
        Assert.IsTrue(entities.Any(entity => entity.EntityKind == "demo:projection" && entity.Id == "projected-01" && entity.IsReadOnly && entity.Source == "demo/repository"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task DeleteAsync_ShouldDeleteNativeEntityByKindAndIdentifier()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        EntityLogic logic = new EntityLogic();
        Entity createdEntity = await logic.UpsertAsync(new Entity()
        {
            Id = "delete-me",
            EntityKind = CoreEntityConstants.Kinds.Status,
            Name = "Delete me"
        });

        bool deleted = await logic.DeleteAsync("CORE:STATUS", "DELETE-ME");
        Entity deletedEntity = await logic.GetByIdAsync(CoreEntityConstants.Kinds.Status, "delete-me");

        Assert.IsNotNull(createdEntity);
        Assert.IsTrue(deleted);
        Assert.IsNull(deletedEntity);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetByIdAsync_ShouldProjectMeshStatusAsReadOnlyEntity()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        AutomationMeshLogic meshLogic = new AutomationMeshLogic();
        await meshLogic.SaveAsync(new AutomationMesh()
        {
            Id = "local",
            PublicId = "mesh-public-id",
            MainSsid = "Maison",
            CurrentScenario = "day",
            CurrentPrivacyMode = AutomationMeshPrivacyMode.High,
            Status = new AutomationMeshStatus()
            {
                GeneralStatusCode = AutomationMeshStatus.StatusPartiallyOK,
                InternetConnectionStatusCode = AutomationMeshStatus.StatusKO
            }
        });

        EntityLogic entityLogic = new EntityLogic();
        Entity projectedEntity = await entityLogic.GetByIdAsync(CoreEntityConstants.Kinds.Status, "LOCAL");

        Assert.IsNotNull(projectedEntity);
        Assert.AreEqual("local", projectedEntity.Id);
        Assert.AreEqual(CoreEntityConstants.Kinds.Status, projectedEntity.EntityKind);
        Assert.IsTrue(projectedEntity.IsReadOnly);
        Assert.AreEqual("mesh/status", projectedEntity.Source);
        Assert.AreEqual("mesh-public-id", projectedEntity.Name);
        Assert.AreEqual(AutomationMeshStatus.StatusPartiallyOK, projectedEntity.Datas["GeneralStatusCode"].SimpleValue);
        Assert.AreEqual(AutomationMeshStatus.StatusKO, projectedEntity.Datas["InternetConnectionStatusCode"].SimpleValue);
        Assert.AreEqual("High", projectedEntity.Datas["PrivacyModeLabel"].SimpleValue);
        Assert.AreEqual("true", projectedEntity.Datas["IsPrivacyModeEnabled"].SimpleValue);
        Assert.AreEqual("day", projectedEntity.Datas["CurrentScenario"].SimpleValue);
        Assert.AreEqual("High", projectedEntity.Datas["CurrentPrivacyMode"].SimpleValue);
        Assert.AreEqual("Maison", projectedEntity.Datas["MainSsid"].SimpleValue);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetByIdAsync_AndGetByKindsAsync_ShouldProjectLocationsAndRoomsAsReadOnlyEntities()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        LocationLogic locationLogic = new LocationLogic();
        Location location = await locationLogic.UpsertAsync(new Location()
        {
            Name = "Home",
            City = "Paris",
            Country = "France",
            HasAutomationsMesh = true,
            Kind = LocationKind.Home,
            Coordinates = new GeoCoordinates() { Latitude = 48.8566M, Longitude = 2.3522M },
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
                            RoomKind = RoomKind.LivingRoom,
                            FloorLevel = 0,
                            Properties = new LocationElementProperties()
                            {
                                Temperature = 21.5M,
                                Humidity = 45.2M,
                                Occupancy = OccupancyState.ActivePresence
                            }
                        }
                    }
                }
            }
        });

        string roomId = location.Zones[0].Rooms[0].Id;
        EntityLogic entityLogic = new EntityLogic();

        Entity projectedLocation = await entityLogic.GetByIdAsync(LocationEntityConstants.Kinds.Location, location.Id.ToUpperInvariant());
        Entity projectedRoom = await entityLogic.GetByIdAsync(LocationEntityConstants.Kinds.Room, roomId);
        List<Entity> projectedRooms = await entityLogic.GetByKindsAsync([LocationEntityConstants.Kinds.Room]);

        Assert.IsNotNull(projectedLocation);
        Assert.AreEqual(LocationEntityConstants.Kinds.Location, projectedLocation.EntityKind);
        Assert.IsTrue(projectedLocation.IsReadOnly);
        Assert.AreEqual("locations/catalog", projectedLocation.Source);
        Assert.AreEqual(location.Id, projectedLocation.Id);
        Assert.AreEqual("Home", projectedLocation.Name);
        Assert.AreEqual("Paris", projectedLocation.Datas["City"].SimpleValue);
        Assert.AreEqual("France", projectedLocation.Datas["Country"].SimpleValue);
        Assert.AreEqual(1L, projectedLocation.Datas["ZoneCount"].IntSimpleValue);
        Assert.AreEqual(1L, projectedLocation.Datas["RoomCount"].IntSimpleValue);

        Assert.IsNotNull(projectedRoom);
        Assert.AreEqual(LocationEntityConstants.Kinds.Room, projectedRoom.EntityKind);
        Assert.IsTrue(projectedRoom.IsReadOnly);
        Assert.AreEqual(location.Id, projectedRoom.LocationId);
        Assert.AreEqual(EntityLogic.NormalizeEntityId(roomId), projectedRoom.Id);
        Assert.AreEqual("Living room", projectedRoom.Name);
        Assert.AreEqual("Home", projectedRoom.Datas["LocationName"].SimpleValue);
        Assert.AreEqual("Ground floor", projectedRoom.Datas["ZoneName"].SimpleValue);
        Assert.AreEqual("LivingRoom", projectedRoom.Datas["RoomKind"].SimpleValue);
        Assert.AreEqual(0L, projectedRoom.Datas["FloorLevel"].IntSimpleValue);
        Assert.AreEqual(21.5M, projectedRoom.Datas["Temperature"].DecimalSimpleValue);
        Assert.AreEqual(45.2M, projectedRoom.Datas["Humidity"].DecimalSimpleValue);
        Assert.AreEqual("ActivePresence", projectedRoom.Datas["Occupancy"].SimpleValue);

        Assert.AreEqual(1, projectedRooms.Count);
        Assert.AreEqual(projectedRoom.Id, projectedRooms[0].Id);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetByIdAsync_AndGetByKindsAsync_ShouldProjectOnlyNonGuestUsersWithoutSensitiveData()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        UserLogic userLogic = new UserLogic();
        User mainUser = await userLogic.UpsertUserAsync("MCARBENAY", new User()
        {
            Name = "CARBENAY",
            FirstName = "Michael",
            CommonName = "Michael",
            MainEmail = "michael@example.test",
            MainPhoneNumber = "+33123456789",
            HashedPassword = "secret-hash",
            HashedPinCode = "pin-hash",
            SsmlTaggedName = "<speak>Michael</speak>",
            IsMain = true,
            Avatar = new UserImageData()
            {
                UrlSquareBig = "https://example.test/users/avatars/mcarbenay/big-square.png"
            }
        });

        await userLogic.UpsertGuestUserAsync(new User()
        {
            Name = "Dupont",
            FirstName = "Jean",
            CommonName = "Jean",
            MainEmail = "jean@example.test"
        });

        EntityLogic entityLogic = new EntityLogic();
        Entity projectedUser = await entityLogic.GetByIdAsync(UserEntityConstants.Kinds.User, "MCARBENAY");
        Entity projectedGuest = await entityLogic.GetByIdAsync(UserEntityConstants.Kinds.User, "dupontjean");
        List<Entity> projectedUsers = await entityLogic.GetByKindsAsync([UserEntityConstants.Kinds.User]);

        Assert.IsNotNull(mainUser);
        Assert.IsNotNull(projectedUser);
        Assert.IsNull(projectedGuest);
        Assert.AreEqual(UserEntityConstants.Kinds.User, projectedUser.EntityKind);
        Assert.AreEqual("mcarbenay", projectedUser.Id);
        Assert.AreEqual("Michael", projectedUser.Name);
        Assert.IsTrue(projectedUser.IsReadOnly);
        Assert.AreEqual("users/catalog", projectedUser.Source);
        Assert.AreEqual("https://example.test/users/avatars/mcarbenay/big-square.png", projectedUser.DefaultImageUrl);
        Assert.AreEqual("https://example.test/users/avatars/mcarbenay/big-square.png", projectedUser.CurrentImageUrl);
        Assert.AreEqual("Michael", projectedUser.Datas["DisplayName"].SimpleValue);
        Assert.AreEqual("Michael", projectedUser.Datas["FirstName"].SimpleValue);
        Assert.AreEqual("CARBENAY", projectedUser.Datas["Name"].SimpleValue);
        Assert.AreEqual("<speak>Michael</speak>", projectedUser.Datas["SsmlTaggedName"].SimpleValue);
        Assert.AreEqual("true", projectedUser.Datas["IsMain"].SimpleValue);
        Assert.IsFalse(projectedUser.Datas.ContainsKey("MainEmail"));
        Assert.IsFalse(projectedUser.Datas.ContainsKey("MainPhoneNumber"));
        Assert.IsFalse(projectedUsers.Any(entity => entity.Id == "dupontjean"));
        Assert.AreEqual(1, projectedUsers.Count);
        Assert.AreEqual(projectedUser.Id, projectedUsers[0].Id);
    }

    private sealed class FakeProjectedEntityRepository : IProjectedEntityRepository
    {
        public string Source => "demo/repository";

        public IReadOnlyCollection<string> SupportedKinds => ["demo:projection"];

        public Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default)
        {
            if (kind != "demo:projection" || entityId != "projected-01")
                return Task.FromResult<Entity>(null);

            return Task.FromResult(new Entity()
            {
                Id = entityId,
                EntityKind = kind,
                Name = "Projected"
            });
        }

        public Task<List<Entity>> GetByKindsAsync(IReadOnlyCollection<string> kinds, CancellationToken cancellationToken = default)
        {
            if (!kinds.Contains("demo:projection"))
                return Task.FromResult(new List<Entity>());

            return Task.FromResult(new List<Entity>()
            {
                new Entity()
                {
                    Id = "projected-01",
                    EntityKind = "demo:projection",
                    Name = "Projected"
                }
            });
        }
    }
}