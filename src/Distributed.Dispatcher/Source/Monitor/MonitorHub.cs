using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace Distributed.Monitor
{
    public class MonitorHub : Hub
    {
        public override Task OnConnected()
        {
            Dispatcher.Instance.Monitor.UpdateJob(Clients.Caller);
            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            return base.OnReconnected();
        }
    }
}
