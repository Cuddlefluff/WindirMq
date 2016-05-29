using System;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using WindirMq.Common;

namespace WindirMq.Client
{
    /// <summary>
    /// 
    /// </summary>
    public interface IClientInfo
    {
        /// <summary>
        /// 
        /// </summary>
        Guid ClientId { get; }
        /// <summary>
        /// 
        /// </summary>
        string NodeName { get; }
        /// <summary>
        /// 
        /// </summary>
        string ServiceName { get; }
        /// <summary>
        /// 
        /// </summary>
        string OperatingSystem { get; }
        /// <summary>
        /// 
        /// </summary>
        string ClientVersion { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ClientInfo : IClientInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public Guid ClientId { get; }
        /// <summary>
        /// 
        /// </summary>
        public string NodeName { get; }
        /// <summary>
        /// 
        /// </summary>
        public string ServiceName { get; }
        /// <summary>
        /// 
        /// </summary>
        public string OperatingSystem { get; }
        /// <summary>
        /// 
        /// </summary>
        public string ClientVersion { get; }

        /// <summary>
        /// 
        /// </summary>
        public ClientInfo()
        {
            ClientId = Guid.NewGuid();
            NodeName = "Anonymous Node";
            ServiceName = "Anonymous Service";
            OperatingSystem = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Runtime.OperatingSystem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        public ClientInfo(Guid clientId) : this()
        {
            ClientId = clientId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="nodeName"></param>
        public ClientInfo(Guid clientId, string nodeName) : this(clientId)
        {
            NodeName = nodeName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="nodeName"></param>
        /// <param name="serviceName"></param>
        public ClientInfo(Guid clientId, string nodeName, string serviceName) : this(clientId, nodeName)
        {
            ServiceName = serviceName;
        }
    }

    /// <summary>
    /// A client which communicates with the message queue server
    /// </summary>
    public sealed class WindirMqClient : IDisposable
    {
        Socket ClientSocket { get; }


        private IMqListener Listener { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IClientInfo ClientInformation { get; }

        /// <summary>
        /// Creates a new client
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="clientInfo"></param>
        public WindirMqClient(EndPoint endpoint, IClientInfo clientInfo)
        {
            ClientInformation = clientInfo;

            ClientSocket = new Socket(SocketType.Stream, ProtocolType.IP);

            ClientSocket.Connect(endpoint);

            SendMessage(new AnnounceMessage() {
                ConversationId = Guid.NewGuid(),
                SenderId = clientInfo.ClientId,
                Created = DateTime.UtcNow,
                ClientVersion = ClientInformation.ClientVersion,
                OperatingSystem = ClientInformation.OperatingSystem,
                NodeName = ClientInformation.NodeName,
                ServiceName = ClientInformation.ServiceName
            });
        }
        /// <summary>
        /// Creates a new client
        /// </summary>
        /// <param name="endpoint"></param>
        public WindirMqClient(EndPoint endpoint) : this(endpoint, new ClientInfo() )
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public async void KeepAlive()
        {
            await ClientSocket.SendAsync(new ArraySegment<byte>(BitConverter.GetBytes((int)CommandType.KeepAlive)), SocketFlags.None);
        }


        /// <summary>
        /// Sends a message asynchronously
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<BaseMessage> SendMessageAsync(BaseMessage message)
        {
            using (var stream = new MemoryStream())
            using (var messageWriter = new MessageWriter(stream, true))
            {
                messageWriter.WriteMessage(message);

                await ClientSocket.SendAsync(new ArraySegment<byte>(stream.ToArray()), SocketFlags.None);

                return message;
            }
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public BaseMessage SendMessage(BaseMessage message)
        {
            using (var stream = new MemoryStream())
            using (var messageWriter = new MessageWriter(stream, true))
            {
                messageWriter.WriteMessage(message);

                ClientSocket.Send(stream.ToArray(), SocketFlags.None);

                return message;


            }

        }

        /// <summary>
        /// Starts listening for incoming messages
        /// </summary>
        /// <param name="onReceived"></param>
        /// <returns></returns>
        public IMqListener Listen(Action<BaseMessage> onReceived)
        {
            if (Listener == null)
            {
                Listener = new MqListener(ClientSocket).Listen(onReceived);
            }
            else
            {
                Listener.Listen(onReceived);
            }

            return Listener;
        }

        private T InitMessage<T>(T msg) where T : BaseMessage
        {
            msg.Created = DateTime.UtcNow;
            msg.SenderId = ClientInformation.ClientId;

            return msg;
        }

        private T InitMessage<T>() where T : BaseMessage, new()
        {
            return InitMessage(new T());
        }

        /// <summary>
        /// Sends a message to the message queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public ContentMessage SendMessage(string queueName, byte[] content, Guid conversationId)
        {
            return (ContentMessage)SendMessage(InitMessage(new ContentMessage()
            {
                Data = content,
                QueueName = queueName,
                ConversationId = conversationId
            }));
        }

        /// <summary>
        /// Sends a message to the message queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public ContentMessage SendMessage(string queueName, byte[] content)
        {
            return SendMessage(queueName, content, Guid.NewGuid());
        }

        /// <summary>
        /// Sends a message to the message queue asynchronously
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public async Task<ContentMessage> SendMessageAsync(string queueName, byte[] content, Guid conversationId)
        {
            var msg = InitMessage(new ContentMessage()
            {
                Data = content,
                QueueName = queueName,
                ConversationId = conversationId
            });

            await SendMessageAsync(msg);

            return msg;
        }

        /// <summary>
        /// Sends a message to the message queue asynchronously
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<ContentMessage> SendMessageAsync(string queueName, byte[] content)
        {
            return SendMessageAsync(queueName, content, Guid.NewGuid());
        }

        /// <summary>
        /// Subscribes to a queue asynchronously
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task<bool> SubscribeAsync(string queueName, SubscribeMode mode = SubscribeMode.TemporaryConsumer)
        {
            return await SendMessageAsync(InitMessage(new SubscriptionMessage()
            {
                QueueName = queueName,
                Mode = mode
            })) != null;
        }

        /// <summary>
        /// Subscribes to a queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool Subscribe(string queueName, SubscribeMode mode = SubscribeMode.TemporaryConsumer)
        {
            return SendMessage(InitMessage(new SubscriptionMessage()
            {
                QueueName = queueName,
                Mode = mode
            })) != null;
        }

        /// <summary>
        /// Unsubscribes from a queue asynchronously. Always ubsubscribe before closing the connection.
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<bool> UnsubscribeAsync(string queueName)
        {
            return await SendMessageAsync(InitMessage(new SubscriptionMessage()
            {
                QueueName = queueName,
                Mode = SubscribeMode.Remove
            })) != null;
        }

        /// <summary>
        /// Unsubscribes from a queue. Always unsubscribe from a queue when you are done with it.
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public bool Unsubscribe(string queueName)
        {
            return SendMessage(InitMessage(new SubscriptionMessage()
            {
                QueueName = queueName,
                Mode = SubscribeMode.Remove
            })) != null;
        }

        /// <summary>
        /// Frees resources used by this instance
        /// </summary>
        public void Dispose()
        {
            SendMessage(InitMessage(new DisconnectMessage()));
            Listener?.Dispose();
        }
    }
}
