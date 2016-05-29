using System;
using System.IO;
using System.Text;

namespace WindirMq.Common
{
    public abstract class BaseMessage
    {
        public Guid SenderId { get; set; }
        public Guid ConversationId { get; set; } = Guid.NewGuid();
        public DateTime Created { get; set; }
        public string QueueName { get; set; }

        protected static readonly Encoding DefaultEncoding = System.Text.Encoding.UTF8;

        public abstract CommandType Type { get; }

        const int MessageBegin = 0x42424242;
        const int MessageEnd = 0x24242424;

        public void WriteMessage(BinaryWriter writer)
        {
            writer.Write(MessageBegin);
            long messageLength;

            using (var tempStream = new MemoryStream())
            using(var internalWriter = new BinaryWriter(tempStream))
            {
                WriteMessageInternal(internalWriter);

                messageLength = tempStream.Position;

                writer.Write(messageLength);

                tempStream.Seek(0L, SeekOrigin.Begin);
                tempStream.CopyTo(writer.BaseStream);
            }

            writer.Write(MessageEnd);
        }

        public void ReadMessage(BinaryReader reader)
        {
            var begin = reader.ReadInt32();

            var offset = reader.BaseStream.Position;


            if (begin != MessageBegin)
            {
                throw new InvalidOperationException("Format mismatch!");
            }

            var messageLength = reader.ReadInt64();

            ReadMessageInternal(reader);

            var end = reader.ReadInt32();

            if (end != MessageEnd)  // this means there are new additions in the messages that aren't supported by the current format. Skip to where the frame ends
            {
                reader.BaseStream.Seek(offset + messageLength, SeekOrigin.Begin);
            }
            

        }

        protected virtual void WriteMessageInternal(BinaryWriter writer)
        {
            writer.Write(SenderId.ToByteArray());
            writer.Write(ConversationId.ToByteArray());
            writer.Write(Created.ToBinary());
            WriteString(writer, QueueName);
        }

        protected virtual void ReadMessageInternal(BinaryReader reader)
        {
            Guid senderId = new Guid(reader.ReadBytes(16));
            Guid messageGuid = new Guid(reader.ReadBytes(16));
            DateTime created = DateTime.FromBinary(reader.ReadInt64());
            var queueName = ReadString(reader);

            ConversationId = messageGuid;
            SenderId = senderId;
            QueueName = queueName;
            Created = created;

        }

        protected string ReadString(BinaryReader reader)
        {
            var strLen = reader.ReadInt32();
            return DefaultEncoding.GetString(reader.ReadBytes(strLen));
        }

        protected void WriteString(BinaryWriter writer, string input)
        {
            var data = input != null ? DefaultEncoding.GetBytes(input) : new byte[] { };
            writer.Write(data.Length);
            writer.Write(data);
        }
    }
}
