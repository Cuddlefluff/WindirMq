using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WindirMq.Common;
using Microsoft.Extensions.Logging;

namespace WindirMq.Server
{
    public class Queue
    {
        public string Name { get; }
        public ConcurrentDictionary<Guid, Subscriber> Subscribers { get; } = new ConcurrentDictionary<Guid, Subscriber>();
        public ConcurrentQueue<MessageWrapper> QueuedMessages { get; } = new ConcurrentQueue<MessageWrapper>();
        public Guid DeclaredBy { get; set; }

        ILogger<Queue> Log { get; } = ServerLog.CreateLogger<Queue>();
        

        IMessagePersistence Persistence { get; } = new FilesystemPersistence();

        public int Pending => QueuedMessages.Count;

        public void Unsubscribe(Guid id)
        {
            Subscriber subscriber;
            if (!Subscribers.TryRemove(id, out subscriber))
                return;

            if(subscriber.Mode == SubscribeMode.PermanentConsumer)
            {
                Persistence.SaveQueue(this);
            }
        }

        public bool HasSubscribed(Guid id)
        {
            return Subscribers.ContainsKey(id);
        }

        public bool IsPermanentSubscriber(Guid id)
        {
            return HasSubscribed(id) && Subscribers[id].Mode == SubscribeMode.PermanentConsumer;
        }

        public void Subscribe(Guid id, SubscribeMode mode)
        {
            Subscribers.AddOrUpdate(id, new Subscriber { Id = id, Mode = mode }, (s, u) => new Subscriber { Id = id, Mode = mode });

            if(mode == SubscribeMode.PermanentConsumer)
                Persistence.SaveQueue(this);
        }

        public Queue(string name)
        {
            Name = name;
            Persistence.SaveQueue(this);
        }

        public void Enqueue(BaseMessage message)
        {
            Enqueue(message, Subscribers.Select(x => x.Key));
        }

        public void Enqueue(BaseMessage message, IEnumerable<Guid> targets)
        {
            var wrapper = new MessageWrapper(message, targets);
            Enqueue(wrapper);
        }

        public void Enqueue(MessageWrapper message, bool persist = true)
        {
            if(persist)
                Persistence.SaveMessage(message);

            QueuedMessages.Enqueue(message);

        }

        public bool Dequeue(out MessageWrapper wrapper)
        {
            return QueuedMessages.TryDequeue(out wrapper);
        }

        public void Purge()
        {
            MessageWrapper msg;

            while(QueuedMessages.TryDequeue(out msg))
            {
                Persistence.DeleteMessage(msg);
            }
        }

        public bool TryProcessDequeue(Func<MessageWrapper, bool> process)
        {

            if (Pending == 0)
                return false;

            MessageWrapper message;

            if (Dequeue(out message))
            {
                try
                {

                    if (process(message))
                    {
                        // this means the message has been successfuly sent to all subscribers
                        // so we can safely delete from persistent storage
                        Log.LogInformation("Message delivered to all receipients. Deleting from persitant storage");
                        Persistence.DeleteMessage(message); 

                        return true;
                    }

                    

                    if(message.NumTries == 0)
                        Log.LogWarning("Message could not be delivered to all receipients, returning to queue");

                    message.NumTries++;

                    Enqueue(message);

                    return false;

                }
                catch (Exception ex)
                {
                    Log.LogError(new EventId(), ex, "An exception occured while trying to deliver message. Message returned to queue.");
                    message.LastException = ex;
                    Enqueue(message);
                    return false;
                }
            }

            return false;
        }

    }
}
