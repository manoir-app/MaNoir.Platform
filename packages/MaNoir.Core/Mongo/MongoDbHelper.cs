using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;

namespace MaNoir.Core.Mongo;

/// <summary>
/// Provides direct access to MongoDB collections using process environment variables.
/// </summary>
public sealed class MongoDbHelper
{
    /// <summary>
    /// The default MongoDB database used by the platform.
    /// </summary>
    public const string DefaultDatabaseName = "home-automation";

    private readonly MongoClient _client;

    static MongoDbHelper()
    {
        BsonSerializer.TryRegisterSerializer<Guid>(new GuidSerializer(GuidRepresentation.CSharpLegacy));
        BsonSerializer.TryRegisterSerializer<DateTimeOffset>(new DateTimeOffsetSerializer(BsonType.Array));
    }

    /// <summary>
    /// Initializes a new helper bound to the platform default database.
    /// </summary>
    public MongoDbHelper()
    {
        _client = new MongoClient(ResolveConnectionString());
        Database = _client.GetDatabase(DefaultDatabaseName);
    }

    /// <summary>
    /// Gets the MongoDB database used by the helper.
    /// </summary>
    public IMongoDatabase Database { get; }

    /// <summary>
    /// Gets a typed collection using the default collection naming convention.
    /// </summary>
    public IMongoCollection<TDocument> GetCollection<TDocument>()
    {
        string collectionName = typeof(TDocument).Name;
        if (!collectionName.EndsWith("s", StringComparison.Ordinal))
        {
            collectionName = collectionName + "s";
        }

        return Database.GetCollection<TDocument>(collectionName);
    }

    /// <summary>
    /// Gets an untyped collection by its explicit MongoDB name.
    /// </summary>
    public IMongoCollection<BsonDocument> GetCollection(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The MongoDB collection name cannot be empty.", nameof(name));
        }

        return Database.GetCollection<BsonDocument>(name);
    }

    private static string ResolveConnectionString()
    {
        string connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        string host = Environment.GetEnvironmentVariable("MONGODB_SERVICE_HOST");
        string portText = Environment.GetEnvironmentVariable("MONGODB_SERVICE_PORT");
        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(portText))
        {
            if (!int.TryParse(portText, out int port) || port <= 0)
            {
                throw new InvalidOperationException("The MONGODB_SERVICE_PORT environment variable must contain a valid TCP port.");
            }

            return string.Format("mongodb://{0}:{1}", host, port);
        }

        throw new InvalidOperationException("MongoDB connection settings are missing. Set MONGODB_CONNECTIONSTRING or both MONGODB_SERVICE_HOST and MONGODB_SERVICE_PORT.");
    }
}