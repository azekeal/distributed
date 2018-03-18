using Common;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public class Agent : IDisposable
    {
        public static Agent Instance { get; private set; }
        public DispatcherConnectionHandler DispatcherConnections { get; private set; }
        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }

        private HubConnection coordinator;
        private IHubContext dispatcherHubContext;
        private IDisposable host;
        private string activeDispatcherName;
        private dynamic ActiveDispatcher;

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
            DispatcherConnections.EndpointAdded += (name, connectionId, info) =>
            {
                if (activeDispatcherName == null)
                {
                    // TODO: groups
                    activeDispatcherName = name;
                    ActiveDispatcher = DispatcherConnections[name].FirstOrDefault();
                }

                foreach (var dispatcher in DispatcherConnections[name])
                {
                    dispatcher.SetAgentIdentifier(Identifier);
                }
            };

            host = WebApp.Start(HostUrl);
            Console.WriteLine("Server running on {0}", HostUrl);
        }

        private void RegisterWithCoordinator()
        {
            coordinator = new HubConnection($"http://localhost:{Constants.Ports.CoordinatorHost}/signalr", Identifier, EndpointData, "AgentHub");
            coordinator.Start();
        }

        internal TaskResult[] StartTasks(IEnumerable<TaskItem> items)
        {
            var tasks = items.ToArray();
            if (tasks.Length > 0)
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
                            data = task.Data
                        });
                    }
                });

                return results;
            }
            else
            {
                return new TaskResult[0] { };
            }
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
