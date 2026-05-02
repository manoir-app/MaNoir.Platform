using Home.Common.Messages;
using NATS.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;

namespace Home.Common;

/// <summary>
/// Represents a NATS request that completed without any response payload.
/// </summary>
[Serializable]
public class NatsNoResponseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NatsNoResponseException"/> class.
    /// </summary>
    public NatsNoResponseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsNoResponseException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public NatsNoResponseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsNoResponseException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Underlying failure.</param>
    public NatsNoResponseException(string message, Exception inner) : base(message, inner)
    {
    }

#pragma warning disable SYSLIB0051
    /// <summary>
    /// Initializes a new instance of the <see cref="NatsNoResponseException"/> class from serialized exception data.
    /// </summary>
    protected NatsNoResponseException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}

/// <summary>
/// Provides convenience helpers for pushing and requesting JSON messages over NATS.
/// </summary>
public static class NatsInterprocess
{
    /// <summary>
    /// Publishes one message using its intrinsic topic.
    /// </summary>
    public static void Push(BaseMessage message)
    {
        Push(message.Topic, message);
    }

    /// <summary>
    /// Publishes one structured message to an explicit topic.
    /// </summary>
    public static void Push(string topic, BaseMessage message)
    {
        string messageContent = JsonConvert.SerializeObject(message);
        Push(topic, messageContent);
    }

    /// <summary>
    /// Publishes one raw message payload to an explicit topic.
    /// </summary>
    public static void Push(string topic, string messageContent)
    {
        for (int index = 0; index < 3; index++)
        {
            try
            {
                using (IConnection connection = GetConnection())
                {
                    connection.Publish(topic, Encoding.UTF8.GetBytes(messageContent));
                    break;
                }
            }
            catch (Exception)
            {
                if (index == 2)
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Sends one request and returns the raw response payload.
    /// </summary>
    public static string Request(string topic, BaseMessage message)
    {
        string messageContent = JsonConvert.SerializeObject(message);
        for (int index = 0; index < 3; index++)
        {
            try
            {
                using (IConnection connection = GetConnection())
                {
                    Msg response = connection.Request(topic, Encoding.UTF8.GetBytes(messageContent), 1500);
                    return Encoding.UTF8.GetString(response.Data, 0, response.Data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send request for {topic} failed ({ex.GetType().Name}): {ex.Message} ");
                Thread.Sleep(500);
                if (index == 2)
                {
                    throw;
                }
            }
        }

        return JsonConvert.SerializeObject(MessageResponse.GenericFail);
    }

    /// <summary>
    /// Sends one request and deserializes the response payload.
    /// </summary>
    public static T Request<T>(string topic, BaseMessage message)
    {
        return Request<T>(topic, message, 15000);
    }

    /// <summary>
    /// Sends one request and deserializes the response payload with a custom timeout.
    /// </summary>
    public static T Request<T>(string topic, BaseMessage message, int durationMaxInMs)
    {
        string messageContent = JsonConvert.SerializeObject(message);
        for (int index = 0; index < 3; index++)
        {
            try
            {
                using (IConnection connection = GetConnection())
                {
                    Msg response = connection.Request(topic, Encoding.UTF8.GetBytes(messageContent), durationMaxInMs);
                    string messageBody = Encoding.UTF8.GetString(response.Data, 0, response.Data.Length);
                    return JsonConvert.DeserializeObject<T>(messageBody);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send request for {topic} failed ({ex.GetType().Name}) : {ex.Message}");
                Thread.Sleep(500);
                if (index == 2)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Creates one transient NATS connection using the configured server list.
    /// </summary>
    public static IConnection GetConnection()
    {
        ConnectionFactory connectionFactory = new ConnectionFactory();
        Options options = ConnectionFactory.GetDefaultOptions();
        options.MaxReconnect = 2;
        options.ReconnectWait = 1000;
        options.Servers = GetServers();
        return connectionFactory.CreateConnection(options);
    }

    /// <summary>
    /// Resolves the configured NATS server endpoints.
    /// </summary>
    public static string[] GetServers()
    {
        string server = "localhost";
        int port = 4222;

        string tmp = Environment.GetEnvironmentVariable("NATS_SERVICE_HOST");
        if (!string.IsNullOrEmpty(tmp))
        {
            server = tmp;
        }

        tmp = Environment.GetEnvironmentVariable("NATS_SERVICE_PORT");
        if (string.IsNullOrEmpty(tmp))
        {
            tmp = Environment.GetEnvironmentVariable("NATS_PORT_4222_TCP_PROTO");
        }

        if (!string.IsNullOrEmpty(tmp) && !int.TryParse(tmp, out port))
        {
            port = 4222;
        }

        return new[] { $"nats://{server}:{port}" };
    }
}