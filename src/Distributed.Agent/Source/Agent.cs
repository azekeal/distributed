using Distributed.Core;
using Distributed.Internal;
using Distributed.Internal.Client;
using Distributed.Internal.Server;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Distributed
{
    public class Agent : IDisposable
    {
        public static Agent Instance { get; private set; }
        public ClientConnectionHandler DispatcherConnections { get; private set; }
        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }

        private dynamic ActiveDispatcher => activeDispatcher != null ? DispatcherConnections[activeDispatcher] : null;

        private HubConnection coordinator;
        private IHubContext dispatcherHubContext;
        private IDisposable host;
        private TaskExecutor taskExecutor;
        private List<string> dispatcherQueue = new List<string>();
        private string activeDispatcher;
        private object lockObj = new object();

        public Agent(TaskExecutor taskExecutor)
        {
            Instance = this;
            Identifier = $"{Constants.Names.Agent}_{Guid.NewGuid()}";
            EndpointData = $"127.0.0.1:{Constants.Ports.AgentHost}";

            this.taskExecutor = taskExecutor ?? throw new NullReferenceException("taskExecutor can't be null");
            this.taskExecutor.Agent = this;

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

            DispatcherConnections = new ClientConnectionHandler(dispatcherHubContext);
            DispatcherConnections.EndpointAdded += AddDispatcher;
            DispatcherConnections.EndpointRemoved += RemoveDispatcher;

            host = WebApp.Start(new StartOptions(HostUrl)
            {
                AppStartup = typeof(AgentStartup).FullName
            });
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
                if (activeDispatcher != null)
                {
                    ActiveDispatcher.SetAgentState(Identifier, false);
                }

                if (dispatcherQueue.Count > 0)
                {
                    activeDispatcher = dispatcherQueue[0];
                    dispatcherQueue.RemoveAt(0);
                }
                else
                {
                    activeDispatcher = null;
                }

                if (activeDispatcher != null)
                {
                    ActiveDispatcher.SetAgentState(Identifier, true);
                }
            }
        }

        internal InitializationResult Initialize(object config)
        {
            var task = taskExecutor.Initialize(config);
            task.Wait();
            return task.Result;
        }

        internal TaskResult[] StartTasks(IEnumerable<TaskItem> tasks)
        {
            var task = taskExecutor.StartTasks(tasks.ToArray());
            task.Wait();
            return task.Result;
        }

        internal void CompleteTask(TaskItem task, TaskResult result)
        {
            ActiveDispatcher.CompleteTask(task, result);
        }
    }
}
