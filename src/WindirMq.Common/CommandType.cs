
namespace WindirMq.Common
{
    public enum CommandType
    {
        Disconnect = -1,
        Unkown = 0,
        SendMessage = 1,
        Subscribe = 2,
        Announce = 3,
        Acknowledge = 4,
        KeepAlive = 65536,
    }
}
