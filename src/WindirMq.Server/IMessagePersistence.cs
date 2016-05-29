using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WindirMq.Common;

namespace WindirMq.Server
{
    public interface IMessagePersistence
    {
        void SaveMessage(MessageWrapper message);
        void DeleteMessage(MessageWrapper message);
        void SaveQueue(Queue queue);
        Queue LoadQueue(string queueName);
        IEnumerable<MessageWrapper> LoadMessages(string queueName);
        IEnumerable<string> GetQueueNames();
    }
}
