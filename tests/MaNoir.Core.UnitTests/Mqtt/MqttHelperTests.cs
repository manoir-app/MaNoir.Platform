using MaNoir.Core.DataPublication;
using MaNoir.Core.Contracts.Models.Entities;
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
}