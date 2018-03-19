using Microsoft.AspNet.SignalR;
using System.Linq;
using System.Threading.Tasks;

namespace Distributed.Internal
{
    public class MonitorHub : Hub
    {
        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(name, message);
        }

        public override Task OnConnected()
        {
            Clients.Caller.setDispatchers(Coordinator.Instance.DispatcherConnections.Endpoints.Values.ToArray());
            Clients.Caller.setAgents(Coordinator.Instance.AgentConnections.Endpoints.Values.ToArray());

            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            return base.OnReconnected();
        }
    }

}
