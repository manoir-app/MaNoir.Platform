using Home.Common.Messages;
using NATS.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;

namespace Home.Common;

[Serializable]
public class NatsNoResponseException : Exception
{
    public NatsNoResponseException()
    {
    }

    public NatsNoResponseException(string message) : base(message)
    {
    }

    public NatsNoResponseException(string message, Exception inner) : base(message, inner)
    {
    }

#pragma warning disable SYSLIB0051
    protected NatsNoResponseException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}

public static class NatsMessageThread
{
    private static bool _shouldStop;

    public static void Stop()
    {
        _shouldStop = true;
    }

    public static void Push(BaseMessage message)
    {
        Push(message.Topic, message);
    }

    public static void Push(string topic, BaseMessage message)
    {
        string messageContent = JsonConvert.SerializeObject(message);
        Push(topic, messageContent);
    }

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

    public static T Request<T>(string topic, BaseMessage message)
    {
        return Request<T>(topic, message, 15000);
    }

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

    public static void Run(string[] topics, MessageHandler handler)
    {
        foreach (string server in GetServers())
        {
            Console.WriteLine(server);
        }

        while (!_shouldStop)
        {
            try
            {
                using (IConnection connection = GetConnection())
                {
                    foreach (string topic in topics)
                    {
                        connection.SubscribeAsync(topic, (sender, args) =>
                        {
                            string messageBody = Encoding.UTF8.GetString(args.Message.Data, 0, args.Message.Data.Length);
                            try
                            {
                                Console.WriteLine("------------------------");
                                Console.Write("Message Recu:");
                                Console.WriteLine(args.Message.Subject);
                                Console.WriteLine("------------------------");

                                MessageResponse response = handler.Invoke(MessageOrigin.Local, args.Message.Subject, messageBody);
                                if (response != null)
                                {
                                    response.Topic = args.Message.Subject;

                                    try
                                    {
                                        Console.WriteLine("------------------------");
                                        Console.Write("Message Recu:");
                                        Console.Write(args.Message.Subject);
                                        Console.Write(" => ");
                                        Console.WriteLine(response.Response);
                                        Console.WriteLine("------------------------");
                                        args.Message.Respond(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }
                                }
                            }
                            catch (NatsNoResponseException)
                            {
                                Console.WriteLine("------------------------");
                                Console.Write("Message Recu:");
                                Console.Write(args.Message.Subject);
                                Console.Write(" => NO RESPONSE");
                                Console.WriteLine("------------------------");
                            }
                            catch (NotImplementedException)
                            {
                                Console.WriteLine("------------------------");
                                Console.Write("Message Recu:");
                                Console.Write(args.Message.Subject);
                                Console.Write(" => NOT IMPLEMENTED");
                                Console.WriteLine("------------------------");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        });
                    }

                    while (!_shouldStop)
                    {
                        Thread.Sleep(500);
                    }
                }
            }
            catch (NotImplementedException)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }
    }

    public static IConnection GetConnection()
    {
        ConnectionFactory connectionFactory = new ConnectionFactory();
        Options options = ConnectionFactory.GetDefaultOptions();
        options.MaxReconnect = 2;
        options.ReconnectWait = 1000;
        options.Servers = GetServers();
        return connectionFactory.CreateConnection(options);
    }

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