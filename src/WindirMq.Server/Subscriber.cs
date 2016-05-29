using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WindirMq.Common;

namespace WindirMq.Server
{
    public sealed class Subscriber
    {
        public Guid Id { get; set; }
        public SubscribeMode Mode { get; set; }
    }
}
