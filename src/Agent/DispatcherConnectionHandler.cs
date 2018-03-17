using Common;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;

namespace Agent
{
    public class DispatcherConnectionHandler : ClientConnectionHandler
    {
        private IHubContext dispatcherHubContext;

        public DispatcherConnectionHandler(IHubContext dispatcherHubContext)
        {
            this.dispatcherHubContext = dispatcherHubContext;
        }

        public IEnumerable<dynamic> this[string name]
        {
            get
            {
                foreach (var connectionId in ConnectionIds[name])
                {
                    // TODO: add all connection to the same user group
                    yield return dispatcherHubContext.Clients.Client(connectionId);
                }
            }
        }
    }
}