using Distributed.Core;
using Distributed.Internal;
using Distributed.Internal.Client;
using Distributed.Internal.Server;
using Distributed.Internal.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Distributed
{
    public sealed class Agent : IDisposable
    {
        public static Agent Instance { get; private set; }
        public ClientConnectionHandler DispatcherConnections { get; private set; }
        public string Identifier { get; private set; }
        public string Endpoint { get; private set; }
        public AgentConfig Config { get; private set; }

        private dynamic ActiveDispatcher => activeDispatcher != null ? DispatcherConnections[activeDispatcher] : null;

        private HubConnection coordinator;
        private IHubContext dispatcherHubContext;
        private IDisposable host;
        private TaskExecutor taskExecutor;
        private List<string> dispatcherQueue = new List<string>();
        private string activeDispatcher;
        private object lockObj = new object();

        public Agent(AgentConfig config, TaskExecutor taskExecutor)
        {
            Instance = this;
            Identifier = $"{Constants.Names.Agent}_{Guid.NewGuid()}";
            Endpoint = $"127.0.0.1:{config.WebPort}"; // TODO: get correct endpoint location
            Config = config;

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

        private void StartListeningForDispatchers()
        {
            dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();

            DispatcherConnections = new ClientConnectionHandler(dispatcherHubContext);
            DispatcherConnections.EndpointAdded += AddDispatcher;
            DispatcherConnections.EndpointRemoved += RemoveDispatcher;

            var hostUrl = GetHostUrl();
            host = WebApp.Start(new StartOptions(hostUrl)
            {
                AppStartup = typeof(AgentStartup).FullName
            });
            Console.WriteLine("Server running on {0}", hostUrl);
        }

        private string GetHostUrl()
        {
            if (Permissions.IsAdministrator())
            {
                return $"http://*:{Config.AgentPort}";
            }
            else
            {
                Console.WriteLine("WARNING: Coordinator needs to be run with admin permissions to be able to serve other computers.");
                return $"http://localhost:{Config.AgentPort}";
            }
        }

        private void RegisterWithCoordinator()
        {
            coordinator = new HubConnection($"http://{Config.CoordinatorAddress}/signalr", Identifier, Endpoint, "AgentHub");
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
