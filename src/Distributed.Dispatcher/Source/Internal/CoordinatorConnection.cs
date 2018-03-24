using Distributed.Internal.Client;
using System;

namespace Distributed.Internal.Dispatcher
{
    public class CoordinatorConnection : EndpointHubConnection
    {
        public CoordinatorConnection(string url, string id, string signalrUrl, string webUrl, string hubName) : base(url, id, signalrUrl, webUrl, hubName)
        {
        }

        public void UpdateJob(Job job)
        {
            try
            {
                if (job != null)
                {
                    var task = Proxy.Invoke("UpdateJob", Identifier, job.Name, job.Priority, job.TaskCount);
                }
                else
                {
                    Proxy.Invoke("ClearJob", Identifier);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"UpdateJob: {e.Message}");
            }
        }

        public void ReleaseAgent(string dispatcherId, string jobId, string agentId)
        {
            try
            {
                Proxy.Invoke("ReleaseAgent", dispatcherId, jobId, agentId);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ReleaseAgent: {e.Message}");
            }
        }
    }
}
