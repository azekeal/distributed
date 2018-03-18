using Common;
using System.Collections.Generic;

namespace Agent
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
