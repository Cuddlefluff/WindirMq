using System;

namespace WindirMq.Common
{ 
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageTypeAttribute : Attribute
    {
        public CommandType Type { get; }

        public MessageTypeAttribute(CommandType type)
        {
            Type = type;
        }
    }
}
