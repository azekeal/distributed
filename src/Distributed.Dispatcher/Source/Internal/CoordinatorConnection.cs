using Distributed.Internal.Client;

namespace Distributed.Internal.Dispatcher
{
    public class CoordinatorConnection : EndpointHubConnection
    {
        public CoordinatorConnection(string url, string id, string signalrUrl, string webUrl, string hubName) : base(url, id, signalrUrl, webUrl, hubName)
        {
        }

        public void SetActiveJob(string name, int priority, int taskCount)
        {
            //Proxy.Invoke("SetActiveJob", identifier, name, priority, taskCount);
        }
    }
}
