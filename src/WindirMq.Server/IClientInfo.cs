using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WindirMq.Server
{
    public interface IClientInfo
    {
        Guid Id { get; set; }
        DateTime Created { get; }
    }

}
