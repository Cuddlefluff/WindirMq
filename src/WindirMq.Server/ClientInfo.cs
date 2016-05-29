using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WindirMq.Server
{
    public class ClientInfo : IClientInfo
    {
        public Guid Id { get; set; } = Guid.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
    }
}
