using Distributed.Core;
using Distributed.Internal.Server;
using System.Collections.Generic;

namespace Distributed.Internal
{
    /// <summary>
    /// Hub for dispatchers connecting to this agent
    /// </summary>
    public class DispatcherHub : EndpointHub
    {
        public DispatcherHub() => ClientConnectionHandler = Agent.Instance.DispatcherConnections;

        public InitializationResult Initialize(object initializationConfig) => Agent.Instance.Initialize(initializationConfig);
        public TaskResult[] StartTasks(IEnumerable<TaskItem> tasks) => Agent.Instance.StartTasks(tasks);
    }
}
