using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;

namespace Distributed.Internal.Server
{
    //[Authorize]
    public class EndpointHub : Hub
    {
        public ClientConnectionHandler ClientConnectionHandler;

        public string Name => Context.Headers["id"];

        public override Task OnConnected()
        {
            Console.WriteLine($"OnConnected {Name}");
            ClientConnectionHandler?.OnConnect(Name, Context.ConnectionId);
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
            ClientConnectionHandler?.OnReconnect(Name, Context.ConnectionId);
            return base.OnReconnected();
        }
    }
}
