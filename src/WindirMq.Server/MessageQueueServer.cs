using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WindirMq.Common;
using Microsoft.Extensions.Logging;

namespace WindirMq.Server
{
    public static class ServerLog
    {
        public static ILoggerFactory LogFactory { get; }

        public static ILogger<T> CreateLogger<T>()
        {
            return LogFactory.CreateLogger<T>();
        }

        static ServerLog()
        {
            LogFactory = new LoggerFactory().AddConsole().AddDebug();
        }
    }

    public class MessageQueueServer : IDisposable
    {
        private Socket ServerSocket { get; }

        private Thread RunThread { get; }

        private Thread QueueProcessThread { get; }

        private IPEndPoint EndPoint { get; }

        private bool Exiting { get; set; }

        private List<MessageQueueClient> Clients { get; } = new List<MessageQueueClient>();

        public List<Queue> Queues { get; } = new List<Queue>();

        private IMessagePersistence Persistence { get; } = new FilesystemPersistence();

        private ILogger<MessageQueueServer> Log { get; } = ServerLog.CreateLogger<MessageQueueServer>();


        public MessageQueueServer(IPEndPoint endpoint)
        {
            ServerSocket = new Socket(SocketType.Stream, ProtocolType.IP);

            EndPoint = endpoint;

            RunThread = new Thread(Run);

            QueueProcessThread = new Thread(ProcessQueue);

        }

        public void Start()
        {
            Log.LogInformation("Loading queues from persistant storage");

            int queueCount = 0;
            int messageCount = 0;

            foreach(var queueName in Persistence.GetQueueNames())
            {
                var queue = Persistence.LoadQueue(queueName);

                Log.LogInformation($"Loading messages for {queueName}");

                foreach(var message in Persistence.LoadMessages(queueName))
                {
                    queue.Enqueue(message);
                    messageCount = 0;
                }

                Queues.Add(queue);

                queueCount++;

            }

            Log.LogInformation($"Loaded {queueCount} queues with a total of {messageCount} messages");

            Log.LogInformation("Starting listener");

            ServerSocket.Bind(EndPoint);

            RunThread.Start();

            QueueProcessThread.Start();
        }

        public void Stop()
        {
            Exiting = true;
        }

        private async void Run()
        {
            ServerSocket.Listen(200);

            while (!Exiting)
            {
                var client = await ServerSocket.AcceptAsync();

                var mqClient = new MessageQueueClient(client, OnMessage);

                Log.LogInformation("Client connected");

                Clients.Add(mqClient);

                mqClient.Start();
            }

            ServerSocket.Shutdown(SocketShutdown.Both);
            ServerSocket.Dispose();
        }

        private MessageQueueClient GetClient(Guid id)
        {
            return Clients.SingleOrDefault(x => x.Info.Id == id);
        }

        private void ProcessQueue()
        {
            while (!Exiting)
            {
                foreach (var queue in Queues.Where(x => x.Pending > 0))
                {
                    for (int i = 0; i < queue.Pending; i++)
                    {
                        queue.TryProcessDequeue(msg =>
                        {
                            if (msg.Lifetime < TimeSpan.FromSeconds(5))
                                return false;

                            var success = true;

                            var successfulSends = new List<Guid>();

                            foreach (var target in msg.RemainingClients)
                            {
                                var client = GetClient(target);

                                if (client == null) // Client is offline. Save message until it's back online.
                                {
                                    success = false;
                                    continue;
                                }

                                client.KeepAlive();
                                
                                if(!client.ClientSocket.Connected)
                                {
                                    Clients.Remove(client);
                                    success = false;
                                    continue;
                                }

                                var result = client.DispatchMessage(msg.Message);

                                if (!result) // Message delivery failed for some reason
                                {
                                    continue;
                                }

                                successfulSends.Add(target);
                            }

                            if (!success)
                            {
                                msg.LastTry = DateTime.UtcNow;
                                msg.Delivered.AddRange(successfulSends);
                            }

                            return success;
                        });
                    }
                }

                Thread.Sleep(1);
            }
        }

        private Queue GetQueue(string name)
        {
            if (!Queues.Any(x => x.Name == name))
            {
                var result = new Queue(name);

                Queues.Add(result);

                return result;
            }

            return Queues.Single(x => x.Name == name);
        }

        private IEnumerable<Queue> GetQueuesByFilter(string filter)
        {
            var regex = new Regex(filter.Replace("*", "[a-z]+"), RegexOptions.IgnoreCase);

            return Queues.Where(x => regex.IsMatch(x.Name));
        }


        private void OnMessage(MessageQueueClient client, BaseMessage message)
        {
            if (message is ContentMessage)
            {

                if (message.QueueName.Contains("*"))
                {
                    Log.LogInformation("Message will be dispatched to wildcard queues");
                    foreach (var queue in GetQueuesByFilter(message.QueueName))
                    {
                        queue.Enqueue(new ContentMessage()
                        {
                            ConversationId = message.ConversationId,
                            Created = message.Created,
                            Data = ((ContentMessage)message).Data,
                            QueueName = queue.Name,
                            SenderId = message.SenderId
                        });
                    }
                }

                GetQueue(message.QueueName).Enqueue(message);
            }
            else if (message is SubscriptionMessage)
            {
                var subMsg = message as SubscriptionMessage;


                if (subMsg.Mode == SubscribeMode.PermanentConsumer || subMsg.Mode == SubscribeMode.TemporaryConsumer)
                {
                    Log.LogInformation($"Client subscription for queue {subMsg.QueueName} with type {subMsg.Mode}");
                    GetQueue(message.QueueName).Subscribe(message.SenderId, subMsg.Mode);
                }
                else if (subMsg.Mode == SubscribeMode.Remove)
                {
                    GetQueue(message.QueueName).Unsubscribe(message.SenderId);
                }
            }
            else if (message is DisconnectMessage)
            {
                if (Clients.Remove(client))
                {
                    foreach (var q in Queues.Where(x => !x.IsPermanentSubscriber(client.Info.Id)))
                    {
                        q.Unsubscribe(client.Info.Id);
                    }

                    client.Dispose();

                    Log.LogInformation($"Client {client.Info.Id} disconnected");
                }
            }
            else if (message is AnnounceMessage)
            {
                var announceMessage = message as AnnounceMessage;

                Log.LogInformation($@"Received announce from client; new Id {message.SenderId}.
Service : {announceMessage.ServiceName}
Node : {announceMessage.NodeName}
Client version : {announceMessage.ClientVersion}
Platform : {announceMessage.OperatingSystem}");
                client.Info.Id = message.SenderId;
            }
        }


        public void Dispose()
        {
            Exiting = true;
        }
    }
}
