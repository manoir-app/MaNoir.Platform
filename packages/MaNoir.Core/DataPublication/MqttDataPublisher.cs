using MaNoir.Core.Contracts.Models.Agents;
using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Users;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MaNoir.Core.DataPublication;

/// <summary>
/// Publishes retained Core mesh and entity snapshots to the configured MQTT broker.
/// </summary>
public static class MqttDataPublisher
{
    private const string DefaultClientName = "core-publication";
    private static IManagedMqttClient _client;

    /// <summary>
    /// Starts the managed MQTT client with the configured broker endpoint.
    /// </summary>
    /// <param name="name">Logical client prefix used to build the MQTT client identifier.</param>
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

    /// <summary>
    /// Stops and disposes the managed MQTT client if it is currently running.
    /// </summary>
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

    /// <summary>
    /// Publishes one retained mesh property value.
    /// </summary>
    /// <param name="property">Mesh property name appended to the MQTT topic.</param>
    /// <param name="value">String payload to retain for the target topic.</param>
    public static void PublishMeshProperty(string property, string value)
    {
        EnsureStarted();
        (string topic, string payload) publication = BuildMeshPropertyPublication(property, value);
        PublishRetained(publication.topic, publication.payload);
    }

    /// <summary>
    /// Publishes the retained MQTT projection of one entity and its flat data values.
    /// </summary>
    /// <param name="entity">Entity snapshot to expose through MQTT.</param>
    public static void PublishEntity(Entity entity)
    {
        EnsureStarted();
        foreach ((string topic, string payload) publication in BuildEntityPublications(entity))
        {
            PublishRetained(publication.topic, publication.payload);
        }
    }

    /// <summary>
    /// Publishes the retained MQTT projection of one registered agent heartbeat snapshot.
    /// </summary>
    /// <param name="agent">Registered agent snapshot to expose through MQTT.</param>
    public static void PublishAgent(RegisteredAgent agent)
    {
        EnsureStarted();
        foreach ((string topic, string payload) publication in BuildAgentPublications(agent))
        {
            PublishRetained(publication.topic, publication.payload);
        }
    }

    /// <summary>
    /// Publishes the retained legacy MQTT topics representing a user's current presence location.
    /// </summary>
    /// <param name="user">The user whose presence should be published.</param>
    /// <param name="currentLocationName">The resolved display name of the current location, when available.</param>
    public static void PublishUserPresence(User user, string currentLocationName = null)
    {
        EnsureStarted();
        foreach ((string topic, string payload) publication in BuildUserPresencePublications(user, currentLocationName))
        {
            PublishRetained(publication.topic, publication.payload);
        }
    }

    /// <summary>
    /// Publishes the aggregate legacy MQTT topics representing the current internet connectivity state.
    /// </summary>
    /// <param name="connection">The updated connectivity snapshot.</param>
    /// <param name="mainSsid">The current main Wi-Fi SSID, when known.</param>
    public static void PublishInternetConnectivityStatus(InternetConnection connection, string mainSsid = null)
    {
        EnsureStarted();
        foreach ((string topic, string payload, bool retain) publication in BuildInternetConnectivityPublications(connection, mainSsid))
        {
            Publish(publication.topic, publication.payload, publication.retain);
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

    internal static List<(string topic, string payload)> BuildAgentPublications(RegisteredAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        string escapedAgentId = EscapeName(agent.AgentId);
        string status = string.IsNullOrWhiteSpace(agent.StatusMessage) ? agent.State.ToString() : agent.StatusMessage;

        return
        [
            ($"manoir/mesh/agents/{escapedAgentId}/status", status),
            ($"manoir/mesh/agents/{escapedAgentId}/lastPing", agent.LastHeartbeatUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture))
        ];
    }

    internal static List<(string topic, string payload)> BuildUserPresencePublications(User user, string currentLocationName = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.IsGuest || string.IsNullOrWhiteSpace(user.Id))
            return [];

        string currentLocationId = ResolveCurrentLocationId(user);

        return
        [
            ($"manoir/mesh/users/{EscapeName(user.Id)}/presence/currentLocation/id", currentLocationId ?? string.Empty),
            ($"manoir/mesh/users/{EscapeName(user.Id)}/presence/currentLocation/name", currentLocationName ?? string.Empty)
        ];
    }

    internal static List<(string topic, string payload, bool retain)> BuildInternetConnectivityPublications(InternetConnection connection, string mainSsid = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        string status = connection.LastMessage ?? connection.Status.ToString();
        List<(string topic, string payload, bool retain)> publications =
        [
            ($"manoir/network/internet-router/status", status, true)
        ];

        if (!string.IsNullOrWhiteSpace(mainSsid))
            publications.Add(($"manoir/network/wifi/mainSsid", mainSsid, true));

        return publications;
    }

    private static void PublishRetained(string topic, string payload)
    {
        Publish(topic, payload, true);
    }

    private static void Publish(string topic, string payload, bool retain)
    {
        MqttApplicationMessage message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        if (retain)
            message.Retain = true;

        _client.EnqueueAsync(message).GetAwaiter().GetResult();
    }

    private static string ResolveCurrentLocationId(User user)
    {
        PresenceLocationData currentLocation = user?.Presence?.Location?
            .Where(location => location != null && location.Probability >= 50)
            .OrderByDescending(location => location.Probability)
            .ThenByDescending(location => location.LatestUpdate)
            .FirstOrDefault();

        return currentLocation?.LocationId;
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
            Start(DefaultClientName);
        }
    }
}