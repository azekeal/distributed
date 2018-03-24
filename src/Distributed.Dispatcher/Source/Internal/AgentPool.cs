using Distributed.Core;
using Distributed.Internal.Client;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Distributed.Internal.Dispatcher
{
    public class AgentPool : EndpointPool, IEnumerable<Agent>
    {
        private Distributed.Dispatcher dispatcher;

        public AgentPool(Distributed.Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public override Endpoint CreateEndpoint(EndpointConnectionInfo info)
        {
            using (Trace.Log())
            {
                return new Agent(dispatcher, info);
            }
        }

        public Agent this[string name] => endpoints.TryGetValue(name, out var endpoint) ? (Agent)endpoint : null;
        public IEnumerable<string> Keys => endpoints.Keys;
        public IEnumerable<Agent> Values => endpoints.Values.Select(e => (Agent)e);

        public IEnumerator<Agent> GetEnumerator() => Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
