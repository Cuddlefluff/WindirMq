using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WindirMq.Common;

namespace WindirMq.Server
{
    public sealed class MessageWrapper
    {
        public BaseMessage Message { get; }
        public List<Guid> Targets { get; } = new List<Guid>();
        public List<Guid> Delivered { get; } = new List<Guid>();
        public DateTime LastTry { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public int NumTries { get; set; }

        public Exception LastException { get; set; }

        public TimeSpan Lifetime => DateTime.UtcNow - LastTry;

        public IEnumerable<Guid> RemainingClients => Targets.Where(x => !Delivered.Contains(x));

        public MessageWrapper(BaseMessage message, IEnumerable<Guid> targets)
        {
            Targets.AddRange(targets);
            Message = message;
        }
    }
}
