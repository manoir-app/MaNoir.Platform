using MaNoir.Core.Contracts.Models.Entities;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Home.Graph.Common;

public static class MqttHelper
{
    private static IManagedMqttClient _client;

    public static void Start(string name)
    {
        if (_client != null)
        {
            return;
        }

        (string server, int port) = ResolveBrokerEndpoint();

        ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithClientId(name + "-" + Environment.MachineName)
                .WithTcpServer(server, port)
                .WithKeepAlivePeriod(TimeSpan.FromMinutes(10))
                .Build())
            .Build();

        _client = new MqttFactory().CreateManagedMqttClient();
        _client.StartAsync(options).GetAwaiter().GetResult();
    }

    public static void Stop()
    {
        if (_client == null)
        {
            return;
        }

        _client.StopAsync().GetAwaiter().GetResult();
        _client.Dispose();
        _client = null;
    }

    public static void PublishMeshProperty(string property, string value)
    {
        EnsureStarted();
        (string topic, string payload) publication = BuildMeshPropertyPublication(property, value);
        PublishRetained(publication.topic, publication.payload);
    }

    public static void PublishEntity(Entity entity)
    {
        EnsureStarted();
        foreach ((string topic, string payload) publication in BuildEntityPublications(entity))
        {
            PublishRetained(publication.topic, publication.payload);
        }
    }

    internal static (string server, int port) ResolveBrokerEndpoint()
    {
        string server = Environment.GetEnvironmentVariable("MQTT_SERVICE_HOST");
        if (string.IsNullOrWhiteSpace(server))
        {
            server = Environment.GetEnvironmentVariable("MOSQUITTO_SERVICE_HOST");
        }

        if (string.IsNullOrWhiteSpace(server))
        {
            server = "localhost";
        }

        int port = 1883;
        string portValue = Environment.GetEnvironmentVariable("MQTT_SERVICE_PORT");
        if (string.IsNullOrWhiteSpace(portValue))
        {
            portValue = Environment.GetEnvironmentVariable("MOSQUITTO_SERVICE_PORT");
        }

        if (!string.IsNullOrWhiteSpace(portValue) && !int.TryParse(portValue, out port))
        {
            port = 1883;
        }

        return (server, port);
    }

    internal static (string topic, string payload) BuildMeshPropertyPublication(string property, string value)
    {
        return ($"manoir/mesh/properties/{property}", value);
    }

    internal static List<(string topic, string payload)> BuildEntityPublications(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        string rootTopic = GetEntityRootTopic(entity.EntityKind);
        string escapedEntityId = EscapeName(entity.Id);

        List<(string topic, string payload)> publications =
        [
            ($"manoir/mesh/{rootTopic}/{escapedEntityId}/name", entity.Name),
            ($"manoir/mesh/{rootTopic}/{escapedEntityId}/kind", entity.EntityKind),
            ($"manoir/mesh/{rootTopic}/{escapedEntityId}/currentImage", entity.CurrentImageUrl ?? entity.DefaultImageUrl)
        ];

        if (entity.Datas != null)
        {
            foreach (KeyValuePair<string, EntityData> data in entity.Datas)
            {
                AddEntityDataPublications(publications, data.Key, data.Value, escapedEntityId, rootTopic);
            }
        }

        return publications;
    }

    private static void PublishRetained(string topic, string payload)
    {
        MqttApplicationMessage message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        _client.EnqueueAsync(message).GetAwaiter().GetResult();
    }

    private static void AddEntityDataPublications(List<(string topic, string payload)> publications, string key, EntityData data, string path, string rootTopic)
    {
        if (data == null)
        {
            return;
        }

        if (IsComplex(data))
        {
            string childPath = path + "/" + EscapeName(key);
            foreach (KeyValuePair<string, EntityData> child in data.ComplexValue)
            {
                AddEntityDataPublications(publications, child.Key, child.Value, childPath, rootTopic);
            }

            return;
        }

        string payload = FormatSimpleValue(data);
        if (payload == null)
        {
            return;
        }

        publications.Add(($"manoir/mesh/{rootTopic}/{path}/{EscapeName(key)}", payload));
    }

    private static string FormatSimpleValue(EntityData data)
    {
        string simpleType = data.SimpleType?.ToLowerInvariant();
        return simpleType switch
        {
            "system.decimal" => data.DecimalSimpleValue?.ToString("0.00", CultureInfo.InvariantCulture),
            "system.int64" => data.IntSimpleValue?.ToString("0", CultureInfo.InvariantCulture),
            "system.datetimeoffset" => data.DateSimpleValue?.ToUniversalTime().ToString("yyyyMMdd-HHmmssZ", CultureInfo.InvariantCulture),
            "system.string" => data.SimpleValue,
            "system.boolean" => data.SimpleValue,
            _ => data.SimpleValue
        };
    }

    private static bool IsComplex(EntityData data)
    {
        return data.ComplexValue != null && data.ComplexValue.Count > 0;
    }

    private static string GetEntityRootTopic(string entityKind)
    {
        if (!string.IsNullOrWhiteSpace(entityKind)
            && entityKind.StartsWith("manoirapp:device/homeautomation", StringComparison.InvariantCultureIgnoreCase))
        {
            return "home-automation";
        }

        return "entities";
    }

    private static string EscapeName(string name)
    {
        return name == null ? "null" : name.Replace("/", "-");
    }

    private static void EnsureStarted()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("MQTT client is not started.");
        }
    }
}