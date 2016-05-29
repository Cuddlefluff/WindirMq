using System.IO;

namespace WindirMq.Common
{
    [MessageType(CommandType.Subscribe)]
    public class SubscriptionMessage : BaseMessage
    {
        public SubscribeMode Mode { get; set; }

        protected override void WriteMessageInternal(BinaryWriter writer)
        {
            base.WriteMessageInternal(writer);
            writer.Write((int)Mode);
        }

        protected override void ReadMessageInternal(BinaryReader reader)
        {
            base.ReadMessageInternal(reader);
            Mode = (SubscribeMode)reader.ReadInt32();
        }

        public override CommandType Type => CommandType.Subscribe;
    }
}
