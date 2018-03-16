using Common;

namespace Dispatcher
{
    public class AgentPool : EndpointPool
    {
        private Dispatcher dispatcher;

        public AgentPool(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public override Endpoint CreateEndpoint(EndpointConnectionInfo info)
        {
            return new Agent(dispatcher.Identifier, dispatcher.EndpointData, info);
        }
    }
}
