
namespace WindirMq.Common
{
    public class AcknowledgeMessage : BaseMessage
    {
        public override CommandType Type => CommandType.Acknowledge;
    }
}
