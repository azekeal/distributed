using Common;
using Microsoft.AspNet.SignalR;

namespace Agent
{
    public class DispatcherConnectionHandler : ClientConnectionHandler
    {
        public DispatcherConnectionHandler(IHubContext dispatcherHubContext) : base(dispatcherHubContext)
        {
        }
    }
}