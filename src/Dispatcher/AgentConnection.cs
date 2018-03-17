using Common;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Dispatcher
{
    using System.Collections.Generic;
    using HubConnection = Common.HubConnection;

    public class AgentConnection : IDisposable
    {
        public event Action<StateChange> StateChanged;
        public event Action<string> SetAgentIdentifier;
        public event Action<TaskItem, TaskResult> TaskCompleted;
        private HubConnection connection;

        public AgentConnection(Dispatcher dispatcher, EndpointConnectionInfo info)
        {
            connection = new HubConnection($"http://localhost:{Constants.Ports.AgentHost}/signalr", dispatcher.Identifier, dispatcher.EndpointData, "DispatcherHub");
            connection.StateChanged += s => StateChanged?.Invoke(s);
            connection.Proxy.On<string>("SetAgentIdentifier", id => SetAgentIdentifier?.Invoke(id));
            connection.Proxy.On<TaskItem, TaskResult>("TaskCompleted", (taskItem, taskResult) => TaskCompleted?.Invoke(taskItem, taskResult));
        }

        public async Task<InitializationResult> Initialize(object initializationConfig)
        {
            return await connection.Proxy.Invoke<InitializationResult>("Initialize", initializationConfig);
        }

        public async Task<TaskResult[]> StartTasks(IEnumerable<TaskItem> tasks)
        {
            return await connection.Proxy.Invoke<TaskResult[]>("StartTasks", tasks);
        }

        public void Start()
        {
            connection.Start();
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
