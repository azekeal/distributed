using Common;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agent
{
    public class DispatcherHub : EndpointHub
    {
        public DispatcherHub() => ClientConnectionHandler = Agent.Instance.DispatcherConnections;

        public InitializationResult Initialize(object initializationConfig) => Agent.Instance.Initialize(initializationConfig);
        public TaskResult[] StartTasks(IEnumerable<TaskItem> tasks) => Agent.Instance.StartTasks(tasks);
    }

    public class Agent : IDisposable
    {
        public static Agent Instance { get; private set; }
        public DispatcherConnectionHandler DispatcherConnections { get; private set; }
        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }

        private dynamic ActiveDispatcher => activeDispatcher != null ? DispatcherConnections[activeDispatcher] : null;

        private HubConnection coordinator;
        private IHubContext dispatcherHubContext;
        private IDisposable host;
        private List<string> dispatcherQueue = new List<string>();
        private string activeDispatcher;
        private object lockObj = new object();

        public Agent()
        {
            Instance = this;
            Identifier = $"{Constants.Names.Agent}_{Guid.NewGuid()}";
            EndpointData = $"127.0.0.1:{Constants.Ports.AgentHost}";

            StartListeningForDispatchers();
            RegisterWithCoordinator();
        }

        public void Dispose()
        {
            coordinator.Dispose();
            host.Dispose();
        }

        public string HostUrl => $"http://localhost:{Constants.Ports.AgentHost}";

        private void StartListeningForDispatchers()
        {
            dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();

            DispatcherConnections = new DispatcherConnectionHandler(dispatcherHubContext);
            DispatcherConnections.EndpointAdded += AddDispatcher;
            DispatcherConnections.EndpointRemoved += RemoveDispatcher;

            host = WebApp.Start(HostUrl);
            Console.WriteLine("Server running on {0}", HostUrl);
        }

        private void RegisterWithCoordinator()
        {
            coordinator = new HubConnection($"http://localhost:{Constants.Ports.CoordinatorHost}/signalr", Identifier, EndpointData, "AgentHub");
            coordinator.Start();
        }

        public void AddDispatcher(string name, string connectionInfo, EndpointConnectionInfo info)
        {
            lock (lockObj)
            {
                dispatcherQueue.Add(name);

                if (activeDispatcher == null)
                {
                    ActivateNextDispatcher();
                }
            }
        }

        public void RemoveDispatcher(string name)
        {
            lock (lockObj)
            {
                dispatcherQueue.Remove(name);

                if (activeDispatcher == name)
                {
                    ActivateNextDispatcher();
                }
            }
        }

        /// <summary>
        /// Currently we only support doing work for a single dispatcher at a time. 
        /// Future work is needed to allow 2+ (configurable) dispatchers with different
        /// runAs to subst
        /// </summary>
        private void ActivateNextDispatcher()
        {
            lock (lockObj)
            {
                if (dispatcherQueue.Count > 0)
                {
                    activeDispatcher = dispatcherQueue[0];
                    dispatcherQueue.RemoveAt(0);
                }
                else
                {
                    activeDispatcher = null;
                }
            }
        }


        internal TaskResult[] StartTasks(IEnumerable<TaskItem> tasks)
        {
            var results = tasks.Select(t => new TaskResult()
            {
                success = true,
                errorMessage = null,
                data = null
            }).ToArray();

            Task.Delay(1000).ContinueWith(t =>
            {
                foreach (var task in tasks)
                {
                    ActiveDispatcher.TaskCompleted(task, new TaskResult()
                    {
                        success = true,
                        errorMessage = null,
                        data = null
                    });
                }
            });

            return results;
        }

        internal InitializationResult Initialize(object initializationConfig)
        {
            return new InitializationResult()
            {
                success = true,
                errorMessage = null,
                capacity = 1
            };
        }
    }
}
