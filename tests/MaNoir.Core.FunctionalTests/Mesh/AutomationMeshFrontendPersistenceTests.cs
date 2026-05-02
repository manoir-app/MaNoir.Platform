using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Mesh;

[TestClass]
[DoNotParallelize]
public sealed class AutomationMeshFrontendPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task FrontendUrlMethods_ShouldPersistAndNormalizeFrontendCatalog()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope connectionScope = new("MONGODB_CONNECTIONSTRING", host.ConnectionString);

        AutomationMeshLogic logic = new AutomationMeshLogic();

        await logic.GetOrCreateLocalAsync("machine-a", "https://localhost:5001");

        Dictionary<string, string> initialFrontendUrls = await logic.GetFrontendUrlsAsync();
        bool invalidChanged = await logic.SetFrontendUrlAsync("adminui", "/relative-only");
        bool adminChanged = await logic.SetFrontendUrlAsync("AdminUi", "https://admin.demo.manoir.app");
        bool adminChangedAgain = await logic.SetFrontendUrlAsync("adminui", "https://admin.demo.manoir.app/");
        bool shellChanged = await logic.SetFrontendUrlAsync("shell", "https://shell.demo.manoir.app/");
        bool adminDeleted = await logic.DeleteFrontendUrlAsync("ADMINUI");
        bool adminDeletedAgain = await logic.DeleteFrontendUrlAsync("adminui");

        Dictionary<string, string> storedFrontendUrls = await logic.GetFrontendUrlsAsync();

        Assert.IsTrue(initialFrontendUrls.ContainsKey("home"));
        Assert.AreEqual("https://localhost:5001/", initialFrontendUrls["home"]);
        Assert.IsFalse(invalidChanged);
        Assert.IsTrue(adminChanged);
        Assert.IsFalse(adminChangedAgain);
        Assert.IsTrue(shellChanged);
        Assert.IsTrue(adminDeleted);
        Assert.IsFalse(adminDeletedAgain);
        Assert.IsFalse(storedFrontendUrls.ContainsKey("adminui"));
        Assert.AreEqual("https://shell.demo.manoir.app/", storedFrontendUrls["shell"]);
        Assert.AreEqual("https://localhost:5001/", storedFrontendUrls["home"]);
    }
}