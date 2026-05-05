using MaNoir.Core.DataPublication;
using MaNoir.Core.Contracts.Models.Agents;
using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace MaNoir.Core.UnitTests.Mqtt;

[TestClass]
public sealed class MqttDataPublisherTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void ResolveBrokerEndpoint_ShouldUseMosquittoEnvironmentVariables()
    {
        string previousMqttHost = Environment.GetEnvironmentVariable("MQTT_SERVICE_HOST");
        string previousMqttPort = Environment.GetEnvironmentVariable("MQTT_SERVICE_PORT");
        string previousMosquittoHost = Environment.GetEnvironmentVariable("MOSQUITTO_SERVICE_HOST");
        string previousMosquittoPort = Environment.GetEnvironmentVariable("MOSQUITTO_SERVICE_PORT");

        try
        {
            Environment.SetEnvironmentVariable("MQTT_SERVICE_HOST", null);
            Environment.SetEnvironmentVariable("MQTT_SERVICE_PORT", null);
            Environment.SetEnvironmentVariable("MOSQUITTO_SERVICE_HOST", "mqtt-broker");
            Environment.SetEnvironmentVariable("MOSQUITTO_SERVICE_PORT", "2883");

            (string server, int port) endpoint = MqttDataPublisher.ResolveBrokerEndpoint();

            Assert.AreEqual("mqtt-broker", endpoint.server);
            Assert.AreEqual(2883, endpoint.port);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MQTT_SERVICE_HOST", previousMqttHost);
            Environment.SetEnvironmentVariable("MQTT_SERVICE_PORT", previousMqttPort);
            Environment.SetEnvironmentVariable("MOSQUITTO_SERVICE_HOST", previousMosquittoHost);
            Environment.SetEnvironmentVariable("MOSQUITTO_SERVICE_PORT", previousMosquittoPort);
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildMeshPropertyPublication_ShouldUseLegacyTopic()
    {
        (string topic, string payload) publication = MqttDataPublisher.BuildMeshPropertyPublication("privacyMode", "none");

        Assert.AreEqual("manoir/mesh/properties/privacyMode", publication.topic);
        Assert.AreEqual("none", publication.payload);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildEntityPublications_ShouldFlattenLegacyTopicsAndPayloads()
    {
        Entity entity = new Entity()
        {
            Id = "device/alpha",
            EntityKind = "manoirapp:device/homeautomation/light",
            Name = "Kitchen",
            DefaultImageUrl = "default.png",
            Datas = new Dictionary<string, EntityData>()
            {
                ["Temperature"] = new EntityData()
                {
                    SimpleType = "System.Decimal",
                    DecimalSimpleValue = 21.5m
                },
                ["UpdatedAt"] = new EntityData()
                {
                    SimpleType = "System.DateTimeOffset",
                    DateSimpleValue = new DateTimeOffset(2026, 4, 25, 10, 30, 45, TimeSpan.FromHours(2))
                },
                ["Flags"] = new EntityData()
                {
                    ComplexValue = new Dictionary<string, EntityData>()
                    {
                        ["State"] = new EntityData()
                        {
                            SimpleType = "System.String",
                            SimpleValue = "on"
                        }
                    }
                }
            }
        };

        List<(string topic, string payload)> publications = MqttDataPublisher.BuildEntityPublications(entity);

        CollectionAssert.AreEquivalent(
        new[]
        {
            "manoir/mesh/home-automation/device-alpha/name=Kitchen",
            "manoir/mesh/home-automation/device-alpha/kind=manoirapp:device/homeautomation/light",
            "manoir/mesh/home-automation/device-alpha/currentImage=default.png",
            "manoir/mesh/home-automation/device-alpha/Temperature=21.50",
            "manoir/mesh/home-automation/device-alpha/UpdatedAt=20260425-083045Z",
            "manoir/mesh/home-automation/device-alpha/Flags/State=on"
        },
        publications.ConvertAll(publication => publication.topic + "=" + publication.payload));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildAgentPublications_ShouldUseLegacyAgentTopics()
    {
        RegisteredAgent agent = new()
        {
            AgentId = "erza",
            State = AgentState.Ready,
            StatusMessage = "Running",
            LastHeartbeatUtc = new DateTimeOffset(2026, 5, 5, 12, 34, 56, TimeSpan.FromHours(2))
        };

        List<(string topic, string payload)> publications = MqttDataPublisher.BuildAgentPublications(agent);

        CollectionAssert.AreEquivalent(
        new[]
        {
            "manoir/mesh/agents/erza/status=Running",
            "manoir/mesh/agents/erza/lastPing=2026-05-05 10:34:56Z"
        },
        publications.ConvertAll(publication => publication.topic + "=" + publication.payload));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildUserPresencePublications_ShouldUseLegacyUserTopics()
    {
        User user = new()
        {
            Id = "michael",
            Presence = new PresenceData()
            {
                Location =
                [
                    new PresenceLocationData()
                    {
                        LocationId = "home",
                        Probability = 85,
                        LatestUpdate = new DateTimeOffset(2026, 5, 5, 18, 0, 0, TimeSpan.Zero)
                    }
                ]
            }
        };

        List<(string topic, string payload)> publications = MqttDataPublisher.BuildUserPresencePublications(user, "Maison");

        CollectionAssert.AreEquivalent(
        new[]
        {
            "manoir/mesh/users/michael/presence/currentLocation/id=home",
            "manoir/mesh/users/michael/presence/currentLocation/name=Maison"
        },
        publications.ConvertAll(publication => publication.topic + "=" + publication.payload));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildInternetConnectivityPublications_ShouldUseAggregateLegacyNetworkTopics()
    {
        InternetConnection connection = new()
        {
            Id = "wan-main",
            IsMain = true,
            Status = ConnectionStatus.Up,
            LastMessage = "Probe OK: https://1.1.1.1/",
            UploadBandwidth = 100,
            DownloadBandwidth = 200,
            UsedUploadBandwidth = 10,
            UsedDownloadBandwidth = 20
        };

        List<(string topic, string payload, bool retain)> publications = MqttDataPublisher.BuildInternetConnectivityPublications(connection, "MaisonWifi");

        CollectionAssert.AreEquivalent(
        new[]
        {
            "manoir/network/internet-router/status=Probe OK: https://1.1.1.1/|True",
            "manoir/network/wifi/mainSsid=MaisonWifi|True"
        },
        publications.ConvertAll(publication => publication.topic + "=" + publication.payload + "|" + publication.retain));
    }
}