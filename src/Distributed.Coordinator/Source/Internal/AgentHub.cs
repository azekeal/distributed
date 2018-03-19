using Distributed.Internal.Server;

namespace Distributed.Internal
{
    public class AgentHub : EndpointHub
    {
        public AgentHub() => ClientConnectionHandler = Coordinator.Instance.AgentConnections;
    }
}
