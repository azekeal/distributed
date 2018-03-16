using Messages;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coordinator
{
    //[Authorize]
    public class DispatcherHub : Hub
    {
        public static DispatcherHub Instance { get; private set; }

        private readonly static ConnectionMapping<string> _connections = new ConnectionMapping<string>();

        public DispatcherHub()
        {
            Instance = this;
        }

        public void AgentAdded(string agentId)
        {
            Clients.All.AgentAdded(agentId);
        }

        public void AgentRemoved(string agentId)
        {
            Clients.All.AgentRemoved(agentId);
        }

        public void AgentListUpdated(List<string> agentIds)
        {
            Clients.All.AgentListUpdated(agentIds);
        }

        public override Task OnConnected()
        {
            string name = Context.User?.Identity?.Name ?? "default";

            _connections.Add(name, Context.ConnectionId);
            Console.WriteLine($"OnConnected {name}");

            if (AgentHub.Instance != null)
            {
                Clients.Caller.AgentListUpdated(AgentHub.Instance?.AgentList.ToList());
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string name = Context.User?.Identity?.Name ?? "default";

            _connections.Remove(name, Context.ConnectionId);
            Console.WriteLine($"OnDisconnected {name}");

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            string name = Context.User?.Identity?.Name ?? "default";

            if (!_connections.GetConnections(name).Contains(Context.ConnectionId))
            {
                _connections.Add(name, Context.ConnectionId);
            }

            Console.WriteLine($"OnReconnectedd {name}");

            return base.OnReconnected();
        }
    }
}
