using System;
using System.Threading.Tasks;
using WindirMq.Common;

namespace WindirMq.Client
{
    /// <summary>
    /// Helper methods for the client
    /// </summary>
    public static class WindirMqClientExtensions
    {
        /// <summary>
        /// Sends a UTF8-encoded string as a message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static ContentMessage SendMessage(this WindirMqClient client, string queueName, string content)
        {
            return client.SendMessage(queueName, System.Text.Encoding.UTF8.GetBytes(content));
        }

        /// <summary>
        /// Sends a UTF8-encoded string as a message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public static ContentMessage SendMessage(this WindirMqClient client, string queueName, string content, Guid conversationId)
        {
            return client.SendMessage(queueName, System.Text.Encoding.UTF8.GetBytes(content), conversationId);
        }

        /// <summary>
        /// Sends an UTF8-encoded string as a message asynchronously
        /// </summary>
        /// <param name="client"></param>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static Task<ContentMessage> SendMessageAsync(this WindirMqClient client, string queueName, string content)
        {
            return client.SendMessageAsync(queueName, System.Text.Encoding.UTF8.GetBytes(content));
        }

        /// <summary>
        /// Sends an UTF8-encoded string as a message asynchronously
        /// </summary>
        /// <param name="client"></param>
        /// <param name="queueName"></param>
        /// <param name="content"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public static Task<ContentMessage> SendMessageAsync(this WindirMqClient client, string queueName, string content, Guid conversationId)
        {
            return client.SendMessageAsync(queueName, System.Text.Encoding.UTF8.GetBytes(content), conversationId);
        }

        /// <summary>
        /// Retrieves the body of a message as an UTF8 encoded string
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string GetMessageBodyString(this ContentMessage message)
        {
            return System.Text.Encoding.UTF8.GetString(message.Data);
        }
    }
}
