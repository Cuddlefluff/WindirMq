using System.IO;
using System.Text;

namespace WindirMq.Common
{
    public class MessageWriter : BinaryWriter
    {
        public MessageWriter(Stream input) : base(input, Encoding.UTF8)
        {
            DefaultEncoding = Encoding.UTF8;
        }

        public MessageWriter(Stream input, bool leaveOpen) : base(input, Encoding.UTF8)
        {
            DefaultEncoding = Encoding.UTF8;
        }

        public MessageWriter(Stream input, bool leaveOpen, Encoding encoding) : base(input, encoding, leaveOpen)
        {
            DefaultEncoding = encoding;
        }

        private Encoding DefaultEncoding { get; }

        public virtual void WriteMessage(BaseMessage message)
        {
            WriteCommandType(message.Type);

            message.WriteMessage(this);
        }

        public void WriteCommandType(CommandType type)
        {
            Write((int)type);
        }
    }
}
