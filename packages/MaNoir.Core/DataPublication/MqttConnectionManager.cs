using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace MaNoir.Core.DataPublication;

/// <summary>
/// Owns the process-wide managed MQTT connection shared by Core publishers and runtime services.
/// </summary>
public sealed class MqttConnectionManager
{
    private readonly object _sync = new object();
    private IManagedMqttClient _client;

    public static MqttConnectionManager Shared { get; } = new MqttConnectionManager();

    public bool IsStarted => _client != null;

    public void Start(string clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("An MQTT client name is required.", nameof(clientName));

        lock (_sync)
        {
            if (_client != null)
                return;

            MqttBrokerOptions options = MqttBrokerOptions.FromEnvironment();
            MqttClientOptionsBuilder clientOptions = new MqttClientOptionsBuilder()
                .WithClientId(string.Concat(clientName.Trim(), "-", Environment.MachineName))
                .WithTcpServer(options.Host, options.Port)
                .WithKeepAlivePeriod(TimeSpan.FromMinutes(10));
            if (!string.IsNullOrWhiteSpace(options.Username))
                clientOptions.WithCredentials(options.Username, options.Password);

            ManagedMqttClientOptions managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions.Build())
                .Build();

            _client = new MqttFactory().CreateManagedMqttClient();
            _client.StartAsync(managedOptions).GetAwaiter().GetResult();
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (_client == null)
                return;

            _client.StopAsync().GetAwaiter().GetResult();
            _client.Dispose();
            _client = null;
        }
    }

    public Task EnqueueAsync(MqttApplicationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        IManagedMqttClient client = _client;
        if (client == null)
            throw new InvalidOperationException("The shared MQTT connection is not started.");

        return client.EnqueueAsync(message);
    }

    internal static (string host, int port, string username, string password) ResolveBrokerOptions()
    {
        MqttBrokerOptions options = MqttBrokerOptions.FromEnvironment();
        return (options.Host, options.Port, options.Username, options.Password);
    }

    private sealed record MqttBrokerOptions(string Host, int Port, string Username, string Password)
    {
        public static MqttBrokerOptions FromEnvironment()
        {
            string host = Environment.GetEnvironmentVariable("MQTT_SERVICE_HOST");
            if (string.IsNullOrWhiteSpace(host))
                host = Environment.GetEnvironmentVariable("MOSQUITTO_SERVICE_HOST");
            if (string.IsNullOrWhiteSpace(host))
                host = "localhost";

            string portValue = Environment.GetEnvironmentVariable("MQTT_SERVICE_PORT");
            if (string.IsNullOrWhiteSpace(portValue))
                portValue = Environment.GetEnvironmentVariable("MOSQUITTO_SERVICE_PORT");

            int port = int.TryParse(portValue, NumberStyles.None, CultureInfo.InvariantCulture, out int configuredPort)
                ? configuredPort
                : 1883;
            return new MqttBrokerOptions(
                host.Trim(),
                port,
                Environment.GetEnvironmentVariable("MQTT_SERVICE_USERNAME"),
                Environment.GetEnvironmentVariable("MQTT_SERVICE_PASSWORD"));
        }
    }
}