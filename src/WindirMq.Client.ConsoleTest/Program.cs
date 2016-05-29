using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace WindirMq.Client.ConsoleTest
{
    using Common;

    public class Program
    {
        static string QueueNameImport { get; set; }
        static string QueueNameBroadcast { get; set; }
        static string FileName { get; set; }

        public static void Main(string[] args)
        {

            Guid clientId = new Guid("{0C79F679-C83C-4B61-96B9-39AC5229C3A3}");

            QueueNameImport = args[0];
            QueueNameBroadcast = args[1];
            FileName = args[2];

            var client = new WindirMqClient(new IPEndPoint(IPAddress.Loopback, 4742), new ClientInfo(clientId, "N/A", "ConsoleTest"));

            Run(client);

            Console.ReadLine();

            client.Dispose();
        }

        private static void Run(WindirMqClient client)
        {
            
            Console.WriteLine($"Subscribing to queue {QueueNameBroadcast}");
            client.Subscribe(QueueNameBroadcast);
            Console.WriteLine("Sending test message");
            var msg = client.SendMessage(QueueNameImport, System.IO.File.ReadAllText(FileName, System.Text.Encoding.UTF8));

            client.Listen((message) => {

                var nmsg = message as ContentMessage;
                Console.WriteLine($"Message received from queue {message.QueueName}");
                Console.WriteLine($"Latency {DateTime.UtcNow - message.Created}");
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(nmsg.Data));
                if(msg.ConversationId == nmsg.ConversationId)
                {
                    Console.WriteLine("Conversation ID's match");
                }

            });

            
        }

        static void OnReceived(BaseMessage message)
        {

            
        }
    }
}
