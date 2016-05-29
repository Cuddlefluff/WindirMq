
namespace WindirMq.Common
{
    [MessageType(CommandType.Disconnect)]
    public class DisconnectMessage : BaseMessage
    {
        public override CommandType Type => CommandType.Disconnect;
    }

}
