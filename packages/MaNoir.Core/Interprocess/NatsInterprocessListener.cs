using Home.Common.Messages;
using NATS.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;

namespace Home.Common;

/// <summary>
/// Hosts a simple long-running NATS subscription loop and dispatches incoming messages to a delegate.
/// </summary>
public static class NatsInterprocessListener
{
    private static bool _shouldStop;

    /// <summary>
    /// Requests the currently running listener loop to stop.
    /// </summary>
    public static void Stop()
    {
        _shouldStop = true;
    }

    /// <summary>
    /// Starts listening to the provided topics until <see cref="Stop"/> is called.
    /// </summary>
    /// <param name="topics">Topics to subscribe to on each configured NATS server.</param>
    /// <param name="handler">Delegate invoked for each received message.</param>
    public static void Run(string[] topics, MessageHandler handler)
    {
        foreach (string server in NatsInterprocess.GetServers())
        {
            Console.WriteLine(server);
        }

        _shouldStop = false;
        while (!_shouldStop)
        {
            try
            {
                using (IConnection connection = NatsInterprocess.GetConnection())
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
}