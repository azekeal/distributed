using Common;
using System.Threading.Tasks;

namespace Dispatcher
{
    public class Coordinator : EndpointHubConnection
    {
        public Coordinator(string url, string id, string endpointData, string hubName) : base(url, id, endpointData, hubName)
        {
        }

        public void SetActiveJob(string name, int priority, int taskCount)
        {
            //Proxy.Invoke("SetActiveJob", identifier, name, priority, taskCount);
        }
    }
}
