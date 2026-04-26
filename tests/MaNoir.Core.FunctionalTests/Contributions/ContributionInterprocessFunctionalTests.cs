using Home.Common;
using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.Contributions;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Secrets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NATS.Client;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Contributions;

[TestClass]
[DoNotParallelize]
public sealed class ContributionInterprocessFunctionalTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task ConfigureContributionInstanceAsync_ShouldRoundTripThroughNatsAndPersistReturnedInstance()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        await using NatsFunctionalTestHost natsHost = new NatsFunctionalTestHost();
        await natsHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope hostScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_HOST", natsHost.Host);
        using ProcessEnvironmentVariableScope portScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_PORT", natsHost.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new ProcessEnvironmentVariableScope("NATS_PORT_4222_TCP_PROTO", null);

        ContributionLogic logic = new ContributionLogic();
        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true, CanInstallMultipleTimes = false }
        ]);

        ContributionInstance instance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "sarah.hue",
            Label = "Hue"
        });

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(natsHost.ConnectionString);
        using IAsyncSubscription subscription = connection.SubscribeAsync("sarah.contribution.configure", (sender, args) =>
        {
            ContributionConfigurationMessage request = BaseMessage.ReadAs<ContributionConfigurationMessage>(Encoding.UTF8.GetString(args.Message.Data));
            ContributionConfigurationResponse response = new ContributionConfigurationResponse(request)
            {
                Response = "ok",
                IsFinalStep = true,
                Instance = request.Instance
            };
            response.Instance.IsConfigured = true;
            response.Instance.Status = ContributionInstanceStatus.Functional;
            response.Instance.StatusMessage = "Bridge reachable.";
            response.Instance.Settings["bridgeIp"] = request.SetupValues["bridgeIp"];

            args.Message.Respond(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response)));
        });
        connection.Flush();

        ContributionConfigurationResponse configured = await logic.ConfigureContributionInstanceAsync(instance.Id, new Dictionary<string, string>()
        {
            ["bridgeIp"] = "192.168.1.20"
        });
        ContributionInstance reloaded = await logic.GetContributionInstanceAsync(instance.Id);

        Assert.IsNotNull(configured);
        Assert.IsNotNull(reloaded);
        Assert.IsTrue(configured.IsFinalStep);
        Assert.IsTrue(reloaded.IsConfigured);
        Assert.AreEqual(ContributionInstanceStatus.Functional, reloaded.Status);
        Assert.AreEqual("Bridge reachable.", reloaded.StatusMessage);
        Assert.AreEqual("192.168.1.20", reloaded.Settings["bridgeIp"]);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task ResolveContributionInstanceSecretsAsync_ShouldReturnAllReferencedSecretsEncryptedForTheRequester()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        await using NatsFunctionalTestHost natsHost = new NatsFunctionalTestHost();
        await natsHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope hostScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_HOST", natsHost.Host);
        using ProcessEnvironmentVariableScope portScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_PORT", natsHost.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new ProcessEnvironmentVariableScope("NATS_PORT_4222_TCP_PROTO", null);
        using ProcessEnvironmentVariableScope apiKeyScope = new ProcessEnvironmentVariableScope("HOMEAUTOMATION_APIKEY", "functional-test-master-key");
        using ProcessEnvironmentVariableScope saltScope = new ProcessEnvironmentVariableScope("HOMEAUTOMATION_SECRETS_SALT", Convert.ToBase64String(new byte[] { 12, 44, 91, 18, 77, 201, 4, 166, 33, 72, 109, 55, 93, 141, 7, 218 }));

        ContributionLogic logic = new ContributionLogic();
        SharedSecretLogic secretLogic = new SharedSecretLogic();
        await secretLogic.SetSecretAsync("hue.client.secret", "super-secret-value");
        await secretLogic.SetSecretAsync("hue.api.key", "another-secret");

        await logic.PublishPluginCatalogAsync(new InstalledPlugin() { Id = "sarah", Label = "Sarah" },
        [
            new ContributionDefinition() { Id = "sarah.hue", Kind = ContributionKind.Integration, Label = "Hue", CanCreateInstances = true, CanInstallMultipleTimes = false }
        ]);

        ContributionInstance pendingInstance = await logic.UpsertContributionInstanceAsync(new ContributionInstance()
        {
            ContributionDefinitionId = "sarah.hue",
            Label = "Hue",
            IsConfigured = true,
            Settings =
            {
                ["clientSecret"] = "{{SECRET: hue.client.secret}}",
                ["apiKey"] = "{{SECRET: hue.api.key }}",
                ["bridgeIp"] = "192.168.1.20"
            }
        });

        ContributionInstance trustedInstance = await logic.AuthorizeContributionInstanceAsync(pendingInstance.Id);

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(natsHost.ConnectionString);
        using IAsyncSubscription subscription = connection.SubscribeAsync("sarah.contribution.secrets.resolve", (sender, args) =>
        {
            ContributionSecretsRequestMessage request = BaseMessage.ReadAs<ContributionSecretsRequestMessage>(Encoding.UTF8.GetString(args.Message.Data));
            ContributionSecretsResponse response = logic.ResolveContributionInstanceSecretsAsync(request.PluginId, request.InstanceId, request.PublicKeyPem).GetAwaiter().GetResult();
            args.Message.Respond(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response)));
        });
        connection.Flush();

        using RSA rsa = RSA.Create(2048);
        string publicKeyPem = rsa.ExportRSAPublicKeyPem();
        ContributionSecretsResponse response = NatsInterprocess.Request<ContributionSecretsResponse>(
            "sarah.contribution.secrets.resolve",
            new ContributionSecretsRequestMessage("sarah", trustedInstance.Id, publicKeyPem),
            5000);

        string clientSecret = UnprotectSecretPayload(response.Secrets["hue.client.secret"], rsa);
        string apiKey = UnprotectSecretPayload(response.Secrets["hue.api.key"], rsa);

        Assert.IsNotNull(response);
        Assert.AreEqual("ok", response.Response);
        Assert.AreEqual(ContributionInstanceStatus.Functional, response.InstanceStatus);
        Assert.AreEqual(2, response.Secrets.Count);
        Assert.AreEqual("super-secret-value", clientSecret);
        Assert.AreEqual("another-secret", apiKey);
    }

    private static string UnprotectSecretPayload(ContributionEncryptedSecretPayload payload, RSA rsa)
    {
        byte[] sessionKey = rsa.Decrypt(Convert.FromBase64String(payload.EncryptedKey), RSAEncryptionPadding.OaepSHA256);
        byte[] nonce = Convert.FromBase64String(payload.Nonce);
        byte[] cipherBytes = Convert.FromBase64String(payload.EncryptedData);
        byte[] authenticationTag = Convert.FromBase64String(payload.AuthenticationTag);
        byte[] plainBytes = new byte[cipherBytes.Length];

        using AesGcm aes = new AesGcm(sessionKey, authenticationTag.Length);
        aes.Decrypt(nonce, cipherBytes, authenticationTag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}