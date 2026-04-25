using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Testcontainers.MongoDb;

namespace MaNoir.Core.FunctionalTests.Infrastructure;

internal sealed class MongoDbFunctionalTestHost : IAsyncDisposable
{
    private readonly MongoDbContainer _container;

    public MongoDbFunctionalTestHost()
    {
        _container = new MongoDbBuilder("mongo:7.0")
            .Build();
    }

    public string ConnectionString
    {
        get { return _container.GetConnectionString(); }
    }

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }

    public IMongoDatabase GetDatabase(string databaseName)
    {
        MongoClient client = new MongoClient(ConnectionString);
        return client.GetDatabase(databaseName);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}