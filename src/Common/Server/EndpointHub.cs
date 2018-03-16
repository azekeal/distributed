using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;

namespace Common
{
    //[Authorize]
    public class EndpointHub : Hub
    {
        public ClientConnectionHandler ClientConnectionHandler;

        private string Name => Context.Headers["id"];
        private string EndpointData => Context.Headers["endpoint"];

        public override Task OnConnected()
        {
            Console.WriteLine($"OnConnected {Name}");
            ClientConnectionHandler?.OnConnect(Name, Context.ConnectionId, EndpointData);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Console.WriteLine($"OnDisconnected {Name}");
            ClientConnectionHandler?.OnDisconnect(Name, Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            Console.WriteLine($"OnReconnected {Name}");
            ClientConnectionHandler?.OnReconnect(Name, Context.ConnectionId, EndpointData);
            return base.OnReconnected();
        }
    }
}
