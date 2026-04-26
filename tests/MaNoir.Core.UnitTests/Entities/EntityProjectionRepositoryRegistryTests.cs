using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Entities;
using MaNoir.Core.Locations;
using MaNoir.Core.Mesh;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.UnitTests.Entities;

[TestClass]
public sealed class EntityProjectionRepositoryRegistryTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void GetRepositoriesForKinds_ShouldMatchKindsIgnoringCase()
    {
        EntityProjectionRepositoryRegistry registry = new EntityProjectionRepositoryRegistry();
        FakeProjectedEntityRepository repository = new FakeProjectedEntityRepository("projection/demo", ["demo:weather", "demo:sun"]);

        registry.Register(repository);

        List<IProjectedEntityRepository> repositories = registry.GetRepositoriesForKinds(["DEMO:SUN"]);

        Assert.AreEqual(1, repositories.Count);
        Assert.AreSame(repository, repositories[0]);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void CreateDefault_ShouldRegisterBuiltInCoreProjections()
    {
        EntityProjectionRepositoryRegistry registry = EntityProjectionRepositoryRegistry.CreateDefault();

        List<IProjectedEntityRepository> statusRepositories = registry.GetRepositoriesForKinds([CoreEntityConstants.Kinds.Status]);
        List<IProjectedEntityRepository> locationRepositories = registry.GetRepositoriesForKinds([LocationEntityConstants.Kinds.Location]);
        List<IProjectedEntityRepository> roomRepositories = registry.GetRepositoriesForKinds([LocationEntityConstants.Kinds.Room]);
        List<IProjectedEntityRepository> userRepositories = registry.GetRepositoriesForKinds([UserEntityConstants.Kinds.User]);

        Assert.AreEqual(1, statusRepositories.Count);
        Assert.IsInstanceOfType<AutomationMeshStatusProjectedEntityRepository>(statusRepositories[0]);
        Assert.AreEqual(1, locationRepositories.Count);
        Assert.IsInstanceOfType<LocationProjectedEntityRepository>(locationRepositories[0]);
        Assert.AreEqual(1, roomRepositories.Count);
        Assert.IsInstanceOfType<LocationProjectedEntityRepository>(roomRepositories[0]);
        Assert.AreEqual(1, userRepositories.Count);
        Assert.IsInstanceOfType<UserProjectedEntityRepository>(userRepositories[0]);
    }

    private sealed class FakeProjectedEntityRepository : IProjectedEntityRepository
    {
        public FakeProjectedEntityRepository(string source, IReadOnlyCollection<string> supportedKinds)
        {
            Source = source;
            SupportedKinds = supportedKinds;
        }

        public string Source { get; }

        public IReadOnlyCollection<string> SupportedKinds { get; }

        public Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Entity>(null);
        }

        public Task<List<Entity>> GetByKindsAsync(IReadOnlyCollection<string> kinds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<Entity>());
        }
    }
}