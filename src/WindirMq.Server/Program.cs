using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using WindirMq.Common;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace WindirMq.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new LoggerFactory().AddConsole().AddDebug();

            var server = new MessageQueueServer(new IPEndPoint(IPAddress.Any, 4742));

            server.Start();

            Console.ReadLine();

            server.Stop();
        }
    }
}
