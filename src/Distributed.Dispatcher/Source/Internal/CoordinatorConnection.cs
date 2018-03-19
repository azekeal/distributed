using Distributed.Internal.Client;

namespace Distributed.Internal.Dispatcher
{
    public class CoordinatorConnection : EndpointHubConnection
    {
        public CoordinatorConnection(string url, string id, string endpointData, string hubName) : base(url, id, endpointData, hubName)
        {
        }

        public void SetActiveJob(string name, int priority, int taskCount)
        {
            //Proxy.Invoke("SetActiveJob", identifier, name, priority, taskCount);
        }
    }
}
