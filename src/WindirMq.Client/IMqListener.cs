using System;
using WindirMq.Common;

namespace WindirMq.Client
{
    /// <summary>
    /// A listener for incoming messages
    /// </summary>
    public interface IMqListener : IDisposable
    {
        /// <summary>
        /// Stops the listener
        /// </summary>
        void Stop();
        /// <summary>
        /// Creates a new listener for this client
        /// </summary>
        /// <param name="onReceived"></param>
        /// <returns></returns>
        IMqListener Listen(Action<BaseMessage> onReceived);
    }
}
