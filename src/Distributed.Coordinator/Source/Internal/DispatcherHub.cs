using Distributed.Internal.Server;

namespace Distributed.Internal
{
    public class DispatcherHub : EndpointHub
    {
        public DispatcherHub() => ClientConnectionHandler = Coordinator.Instance.DispatcherConnections;
    }
}
