using DotNet.Testcontainers.Configurations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Infrastructure;

[TestClass]
public sealed class FunctionalTestAssemblyHooks
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        // A test container that never becomes ready must fail the test instead of blocking the run.
        // The limit starts once the container does, so it does not include the image download.
        TestcontainersSettings.WaitStrategyTimeout = TimeSpan.FromSeconds(60);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await MosquittoFunctionalTestHost.DisposeSharedAsync();
        await NatsFunctionalTestHost.DisposeSharedAsync();
        await MongoDbFunctionalTestHost.DisposeSharedAsync();
    }
}