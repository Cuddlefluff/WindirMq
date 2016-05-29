using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using WindirMq.Common;

namespace WindirMq.Server
{
    public class FilesystemPersistence : IMessagePersistence
    {

        public FilesystemPersistence()
        {

        }

        public void DeleteMessage(MessageWrapper message)
        {
            File.Delete(GetMessagePath(message));
        }

        public void SaveMessage(MessageWrapper message)
        {
            var queuePath = GetQueuePath(message.Message.QueueName);

            if (!Directory.Exists(queuePath))
                Directory.CreateDirectory(queuePath);


            using (var stream = new FileStream(GetMessagePath(message), FileMode.Create, FileAccess.Write, FileShare.Read))
            using(var writer = new MessageWriter(stream))
            {
                writer.WriteMessage(message.Message);
                foreach(var client in message.RemainingClients)
                {
                    writer.Write(client.ToByteArray());
                }
            }
        }

        public void SaveQueue(Queue queue)
        {
            var queuePath = GetQueuePath(queue.Name);

            if (!Directory.Exists(queuePath))
                Directory.CreateDirectory(queuePath);


            using (var stream = new FileStream(string.Concat(queuePath, Path.DirectorySeparatorChar, "queue.dat"), FileMode.Create, FileAccess.Write, FileShare.Read))
            using(var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8))
            {
                writer.Write(queue.DeclaredBy.ToByteArray());
                writer.Write(queue.Name);
                foreach(var q in queue.Subscribers.Select(x => x.Key.ToByteArray().Concat(BitConverter.GetBytes((int)x.Value.Mode))))
                {
                    writer.Write(q.ToArray());
                }
            }
        }

        public IEnumerable<MessageWrapper> LoadMessages(string queueName)
        {
            var queuePath = GetQueuePath(queueName);

            if (!Directory.Exists(queuePath))
                yield break;

            foreach(var file in Directory.GetFiles(queuePath, "*.msg"))
            {
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using(var reader = new MessageReader(fileStream))
                {
                    var msg = reader.ReadMessage();
                    var targets = new List<Guid>();

                    while(fileStream.Position < fileStream.Length)
                    {
                        targets.Add(new Guid(reader.ReadBytes(16)));
                    }

                    var fname = Path.GetFileName(file);
                    Guid id = new Guid(fname.Substring(0, fname.IndexOf('.')));

                    yield return new MessageWrapper(msg, targets) { Id = id };

                }
            }


        }

        public Queue LoadQueue(string queueName)
        {
            var queuePath = GetQueuePath(queueName);

            if (!Directory.Exists(queuePath))
                return null;

            using (var stream = new FileStream(string.Concat(queuePath, Path.DirectorySeparatorChar, "queue.dat"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8))
            {
                

                var declaredBy = new Guid(reader.ReadBytes(16));
                var name = reader.ReadString();

                var result = new Queue(name);

                while(stream.Position < stream.Length)
                {
                    var subscriberId = new Guid(reader.ReadBytes(16));
                    var mode = (SubscribeMode)reader.ReadInt32();

                    result.Subscribe(subscriberId, mode);
                }

                return result;

            }
        }

        public IEnumerable<string> GetQueueNames()
        {
            if (!Directory.Exists(GetBaseQueuePath()))
                return Enumerable.Empty<string>();

            return new  DirectoryInfo(GetBaseQueuePath()).GetDirectories().Select(x => x.Name);

        }

        private string GetMessagePath(MessageWrapper message)
        {
            return string.Concat( GetQueuePath(message.Message.QueueName), Path.DirectorySeparatorChar, message.Id,  ".msg");
        }

        private string GetBaseQueuePath()
        {
            return string.Concat("data", Path.DirectorySeparatorChar, "queues");
        }

        private string GetQueuePath(string queueName)
        {
            return string.Concat(GetBaseQueuePath(), Path.DirectorySeparatorChar, queueName);
        }
    }
}
