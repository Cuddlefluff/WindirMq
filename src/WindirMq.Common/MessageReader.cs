using System;
using System.IO;
using System.Text;

namespace WindirMq.Common
{
    public class MessageReader : BinaryReader
    {
        public MessageReader(Stream input) : base(input, Encoding.UTF8)
        {
            DefaultEncoding = Encoding.UTF8;
        }

        public MessageReader(Stream input, bool leaveOpen) : base(input, Encoding.UTF8, leaveOpen)
        {
            DefaultEncoding = Encoding.UTF8;
        }

        public MessageReader(Stream input, bool leaveOpen, Encoding encoding) : base(input, encoding, leaveOpen)
        {
            DefaultEncoding = encoding;
        }

        private Encoding DefaultEncoding { get; }

        private T CreateReadMessage<T>() where T : BaseMessage, new()
        {
            var result = new T();

            result.ReadMessage(this);

            return result;
        }

        public BaseMessage ReadMessage()
        {
            var commandType = ReadCommandType();

            if(commandType == CommandType.KeepAlive)
            {
                return new KeepAliveMessage();
            }

            if (commandType == CommandType.SendMessage)
            {
                return CreateReadMessage<ContentMessage>();
            }
            else if (commandType == CommandType.Subscribe)
            {
                return CreateReadMessage<SubscriptionMessage>();
            }
            else if (commandType == CommandType.Disconnect)
            {
                return CreateReadMessage<DisconnectMessage>();
            }
            else if (commandType == CommandType.Announce)
            {
                return CreateReadMessage<AnnounceMessage>();
            }

            throw new InvalidOperationException("Unknown message");

        }

        public CommandType ReadCommandType()
        {
            return (CommandType)ReadInt32();
        }
    }
}
