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
        public string SignalrUrl => Context.Headers["signalrUrl"];
        public string WebUrl => Context.Headers["webUrl"];

        public override Task OnConnected()
        {
            Console.WriteLine($"OnConnected {Name}");
            ClientConnectionHandler?.OnConnect(Name, Context.ConnectionId, SignalrUrl, WebUrl);
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
            ClientConnectionHandler?.OnReconnect(Name, Context.ConnectionId, SignalrUrl, WebUrl);
            return base.OnReconnected();
        }
    }
}
