using Distributed.Core;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HubConnection = Distributed.Internal.Client.HubConnection;

namespace Distributed.Internal.Dispatcher
{
    public class AgentConnection : IDisposable
    {
        public const int MaxRetryCount = 5;

        public event Action<StateChange> StateChanged;
        public event Action<string, bool> SetAgentState;
        public event Action<TaskItem, TaskResult> TaskCompleted;

        private HubConnection connection;
        private ManualResetEvent stopped = new ManualResetEvent(false);
        private ConnectionState currentState;
        private bool disposed;
        private int retryCount = 0;

        public AgentConnection(Distributed.Dispatcher dispatcher, EndpointConnectionInfo info)
        {
            using (Trace.Log())
            {
                connection = new HubConnection($"http://{info.signalrUrl}/", dispatcher.Identifier, dispatcher.SignalrUrl, dispatcher.WebUrl, "DispatcherHub");
                connection.StateChanged += s =>
                {
                    currentState = s.NewState;
                    StateChanged?.Invoke(s);

                    if (currentState == ConnectionState.Disconnected)
                    {
                        if (s.OldState == ConnectionState.Connecting)
                        {
                            if (retryCount++ > MaxRetryCount)
                            {
                                connection.Stop();
                                dispatcher.ReleaseAgent(info.name);
                            }
                        }

                        if (disposed)
                        {
                            stopped.Set();
                        }
                    }
                };
                connection.Proxy.On<string, bool>("SetAgentState", (id, active) => SetAgentState?.Invoke(id, active));
                connection.Proxy.On<TaskItem, TaskResult>("CompleteTask", (taskItem, taskResult) => TaskCompleted?.Invoke(taskItem, taskResult));
            }
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
            using (Trace.Log())
            {
                connection.Start();
            }
        }

        public void Dispose()
        {
            using (Trace.Log())
            {
                if (currentState == ConnectionState.Disconnected)
                {
                    stopped.Set();
                }

                disposed = true;
                connection.Stop();
                stopped.WaitOne();

                connection.Dispose();
            }
        }
    }
}
