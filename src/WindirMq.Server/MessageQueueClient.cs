using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WindirMq.Common;

namespace WindirMq.Server
{
    public class MessageQueueClient : IDisposable
    {

        public Socket ClientSocket { get; }

        public Thread RunThread { get; }

        private bool Exiting { get; set; }

        public IClientInfo Info { get; }

        private Action<MessageQueueClient, BaseMessage> OnMessage { get; }

        public MessageQueueClient(Socket socket, Action<MessageQueueClient, BaseMessage> onMessage)
        {
            ClientSocket = socket;
            RunThread = new Thread(Run);
            Info = new ClientInfo() { Created = DateTime.UtcNow };
            OnMessage = onMessage;
        }

        public bool DispatchMessage(BaseMessage message)
        {
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new MessageWriter(memoryStream))
            {
                streamWriter.WriteMessage(message);

                try
                {
                    ClientSocket.Send(memoryStream.ToArray(), SocketFlags.None);

                    return true;
                }
                catch (ObjectDisposedException)
                {
                    Stop();

                    return false;
                }
                catch (SocketException)
                {
                    Stop();
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public void Start()
        {
            RunThread.Start();
        }

        public void Stop()
        {
            Exiting = true;
        }


        private void Run()
        {
            byte[] buffer = new byte[65536];

            bool selfDisconnected = false;

            while (!Exiting)
            {

                if (ClientSocket.Available == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                int bytesRead;

                try
                {
                    bytesRead = ClientSocket.Receive(buffer);
                }
                catch (SocketException)
                {
                    Stop();
                    break;
                }

                using (var memoryStream = new MemoryStream())
                {

                    memoryStream.Write(buffer, 0, bytesRead);

                    while (ClientSocket.Available > 0 && (bytesRead = ClientSocket.Receive(buffer)) != 0)
                    {
                        memoryStream.Write(buffer, 0, bytesRead);
                    }

                    memoryStream.Seek(0L, SeekOrigin.Begin);

                    using (var messageReader = new MessageReader(memoryStream, false))
                    {
                        do
                        {
                            var msg = messageReader.ReadMessage();

                            if (msg is DisconnectMessage)
                                selfDisconnected = true;

                            if (msg is KeepAliveMessage)
                                continue;

                            OnMessage(this, msg);

                        } while (memoryStream.Position < memoryStream.Length);
                    }
                }
            }

            if (!selfDisconnected)
                OnMessage(this, new DisconnectMessage());

        }

        public void KeepAlive()
        {
            ClientSocket.Send(BitConverter.GetBytes((int)CommandType.KeepAlive));
        }

        public void Dispose()
        {
            Exiting = true;
            try
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}
