using MaNoir.Core.DataAccess;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Secrets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Secrets;

[TestClass]
[DoNotParallelize]
public sealed class SharedSecretPersistenceTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task SetSecretAsync_ShouldPersistEncryptedPayloadAndRoundTripClearText()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);
        using ProcessEnvironmentVariableScope apiKeyScope = new ProcessEnvironmentVariableScope("HOMEAUTOMATION_APIKEY", "functional-test-master-key");
        using ProcessEnvironmentVariableScope saltScope = new ProcessEnvironmentVariableScope("HOMEAUTOMATION_SECRETS_SALT", Convert.ToBase64String(new byte[] { 12, 44, 91, 18, 77, 201, 4, 166, 33, 72, 109, 55, 93, 141, 7, 218 }));

        SharedSecretLogic logic = new SharedSecretLogic();

        await logic.SetSecretAsync("Hue.ClientSecret", "super-secret-value");
        string clearText = await logic.GetSecretAsync("hue.clientsecret");

        MongoDbHelper mongo = new MongoDbHelper();
        BsonDocument storedDocument = await mongo.GetCollection("SharedSecrets").Find(new BsonDocument("_id", "hue.clientsecret")).FirstOrDefaultAsync();

        Assert.AreEqual("super-secret-value", clearText);
        Assert.IsNotNull(storedDocument);
        Assert.AreEqual(SharedSecret.EncryptionModeAes256GcmPbkdf2Sha256V1, storedDocument["EncryptionMode"].AsString);
        Assert.AreNotEqual("super-secret-value", storedDocument["EncryptedData"].AsString);
        Assert.IsTrue(storedDocument.Contains("Nonce"));
        Assert.IsTrue(storedDocument.Contains("AuthenticationTag"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task SetSecretAsync_ShouldFailWhenProtectionEnvironmentIsMissing()
    {
        await using MongoDbFunctionalTestHost host = new MongoDbFunctionalTestHost();
        await host.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", host.ConnectionString);
        using ProcessEnvironmentVariableScope apiKeyScope = new ProcessEnvironmentVariableScope("HOMEAUTOMATION_APIKEY", null);
        using ProcessEnvironmentVariableScope saltScope = new ProcessEnvironmentVariableScope("HOMEAUTOMATION_SECRETS_SALT", null);

        SharedSecretLogic logic = new SharedSecretLogic();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => logic.SetSecretAsync("missing-env", "value"));
    }
}