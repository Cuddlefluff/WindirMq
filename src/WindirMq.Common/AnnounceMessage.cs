
using System.IO;

namespace WindirMq.Common
{
    public class AnnounceMessage : BaseMessage
    {
        public override CommandType Type => CommandType.Announce;

        public string ServiceName { get; set; }
        public string NodeName { get; set; }
        public string OperatingSystem { get; set; }
        public string ClientVersion { get; set; }

        protected override void WriteMessageInternal(BinaryWriter writer)
        {
            base.WriteMessageInternal(writer);

            WriteString(writer, ServiceName);
            WriteString(writer, NodeName);
            WriteString(writer, OperatingSystem);
            WriteString(writer, ClientVersion);
        }

        protected override void ReadMessageInternal(BinaryReader reader)
        {
            base.ReadMessageInternal(reader);

            ServiceName = ReadString(reader);
            NodeName = ReadString(reader);
            OperatingSystem = ReadString(reader);
            ClientVersion = ReadString(reader);
        }
    }
}
