using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using WindirMq.Common;

namespace WindirMq.Client
{
    /// <summary>
    /// Listener for the message queue client
    /// </summary>
    public sealed class MqListener : IMqListener
    {
        private Thread ListenThread { get; }

        private event Action<BaseMessage> OnReceived;

        private Socket ClientSocket { get; }

        private bool Exiting { get; set; }

        internal MqListener(Socket clientSocket)
        {
            ClientSocket = clientSocket;

            ListenThread = new Thread(Listen);

            ListenThread.Start();
        }

        private void Listen()
        {
            // internal buffer
            byte[] buffer = new byte[65536];
            while (!Exiting)
            {
                /*if (ClientSocket.Available == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }
                */
                int readCount;

                try
                {
                    readCount = ClientSocket.Receive(buffer, SocketFlags.None);
                }
                catch(SocketException)
                {
                    Exiting = true;
                    break;
                }
                

                if (readCount == 0)
                {
                    continue;
                }

                using (var stream = new MemoryStream())
                {
                    stream.Write(buffer, 0, readCount);

                    while (ClientSocket.Available > 0 && (readCount = ClientSocket.Receive(buffer, SocketFlags.None)) != 0)
                    {
                        stream.Write(buffer, 0, readCount);
                    }

                    stream.Seek(0L, SeekOrigin.Begin);

                    using (var messageReader = new MessageReader(stream))
                    {
                        var message = messageReader.ReadMessage();

                        if(message is KeepAliveMessage)
                        {
                            ClientSocket.Send(BitConverter.GetBytes((int)CommandType.KeepAlive));
                        }

                        OnReceived(message);
                        
                    }

                }
            }

            Dispose();
        }

        /// <summary>
        /// Adds a new delegate to the listener
        /// </summary>
        /// <param name="onReceived"></param>
        /// <returns></returns>
        public IMqListener Listen(Action<BaseMessage> onReceived)
        {
            OnReceived += onReceived;

            return this;
        }

        /// <summary>
        /// Stops the client
        /// </summary>
        public void Stop()
        {
            Exiting = true;
        }

        /// <summary>
        /// Disposes any resources used by this instance
        /// </summary>
        public void Dispose()
        {
            Stop();
            try
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Dispose();
            }
            catch(ObjectDisposedException)
            {
                // ignore
            }

        }
    }
}
