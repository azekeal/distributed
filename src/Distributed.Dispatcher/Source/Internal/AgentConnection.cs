using Distributed.Core;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HubConnection = Distributed.Internal.Client.HubConnection;

namespace Distributed.Internal.Dispatcher
{
    public class AgentConnection : IDisposable
    {
        public event Action<StateChange> StateChanged;
        public event Action<string, bool> SetAgentState;
        public event Action<TaskItem, TaskResult> TaskCompleted;
        private HubConnection connection;

        public AgentConnection(Distributed.Dispatcher dispatcher, EndpointConnectionInfo info)
        {
            connection = new HubConnection($"http://{info.signalrUrl}/", dispatcher.Identifier, dispatcher.SignalrUrl, dispatcher.WebUrl, "DispatcherHub");
            connection.StateChanged += s => StateChanged?.Invoke(s);
            connection.Proxy.On<string, bool>("SetAgentState", (id, active) => SetAgentState?.Invoke(id, active));
            connection.Proxy.On<TaskItem, TaskResult>("CompleteTask", (taskItem, taskResult) => TaskCompleted?.Invoke(taskItem, taskResult));
        }

        public async Task<InitializationResult> Initialize(object initializationConfig)
        {
            return await connection.Proxy.Invoke<InitializationResult>("Initialize", initializationConfig);
        }

        public async Task<TaskResult[]> StartTasks(IEnumerable<TaskItem> tasks)
        {
            Console.WriteLine("Invoke: connection.StartTasks: " + string.Join(", ", tasks.Select(t => t.ToString())));
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
