using System.IO;

namespace WindirMq.Common
{
    [MessageType(CommandType.SendMessage)]
    public class ContentMessage : BaseMessage
    {
        public string Topic { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }

        protected override void WriteMessageInternal(BinaryWriter writer)
        {
            base.WriteMessageInternal(writer);

            var topicData = Topic != null ? DefaultEncoding.GetBytes(Topic) : new byte[] { };
            var contentTypeData = ContentType != null ? DefaultEncoding.GetBytes(ContentType) : new byte[] { };

            WriteString(writer, ContentType);

            WriteString(writer, Topic);

            writer.Write((long)Data.Length);
            writer.Write(Data);
        }

        protected override void ReadMessageInternal(BinaryReader reader)
        {
            base.ReadMessageInternal(reader);

            ContentType = ReadString(reader);

            Topic = ReadString(reader);

            var dataLen = reader.ReadInt64();
            if (dataLen > 0L)
                Data = reader.ReadBytes((int)dataLen);

        }

        public override CommandType Type => CommandType.SendMessage;
    }
}
