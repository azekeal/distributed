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
    public class AgentHub : Hub
    {
        public static AgentHub Instance { get; private set; }

        private readonly static ConnectionMapping<string> _connections = new ConnectionMapping<string>();

        public IEnumerable<string> AgentList => _connections.Keys;

        public AgentHub()
        {
            if (Instance == null)
            {
                Instance = this;
                DispatcherHub.Instance?.AgentListUpdated(AgentList.ToList());
            }
        }

        public void SendChatMessage(string from, string msg)
        {
            foreach (var c in _connections.GetConnections(from))
            {
                Clients.All.SendChatMessage($"{from}: {msg}");
            }
        }

        public override Task OnConnected()
        {
            string name = Context.User?.Identity?.Name ?? "default";

            if (_connections.Add(name, Context.ConnectionId))
            {
                DispatcherHub.Instance?.AgentAdded(name);
            }

            Console.WriteLine($"OnConnected {name}");

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string name = Context.User?.Identity?.Name ?? "default";

            if (_connections.Remove(name, Context.ConnectionId))
            {
                DispatcherHub.Instance.AgentRemoved(name);
            }

            Console.WriteLine($"OnDisconnected {name}");

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            string name = Context.User?.Identity?.Name ?? "default";

            if (!_connections.GetConnections(name).Contains(Context.ConnectionId))
            {
                if (_connections.Add(name, Context.ConnectionId))
                {
                    DispatcherHub.Instance.AgentAdded(name);
                }
            }

            Console.WriteLine($"OnReconnectedd {name}");

            return base.OnReconnected();
        }
    }
}
