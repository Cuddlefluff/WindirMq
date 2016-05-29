
namespace WindirMq.Common
{
    public class KeepAliveMessage : BaseMessage
    {
        public override CommandType Type => CommandType.KeepAlive;
    }
}
