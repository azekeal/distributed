using Distributed.Internal.Server;

namespace Distributed.Internal
{
    public class DispatcherHub : EndpointHub
    {
        public DispatcherHub() => ClientConnectionHandler = Coordinator.Instance.DispatcherConnections;

        public void UpdateJob(string dispatcherId, string jobId, int jobPriority, int jobTaskCount)
        {
            Coordinator.Instance.UpdateJob(dispatcherId, jobId, jobPriority, jobTaskCount);
        }

        public void ClearJob(string dispatcherId)
        {
            Coordinator.Instance.ClearJob(dispatcherId);
        }

        public void ReleaseAgent(string dispatcherId, string jobId, string agentId)
        {
            Coordinator.Instance.ReleaseAgent(dispatcherId, jobId, agentId);
        }
    }
}
