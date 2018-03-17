using Common;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dispatcher
{
    public class AgentPool : EndpointPool, IEnumerable<Agent>
    {
        private Dispatcher dispatcher;

        public AgentPool(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public override Endpoint CreateEndpoint(EndpointConnectionInfo info)
        {
            return new Agent(dispatcher, info);
        }

        public IEnumerator<Agent> GetEnumerator() => endpoints.Values.Select(endpoint => (Agent)endpoint).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
