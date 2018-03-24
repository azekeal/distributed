using Distributed.Core;
using Distributed.Internal;
using Distributed.Internal.Client;
using Distributed.Internal.Server;
using Distributed.Internal.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Distributed
{
    public sealed class Agent : IDisposable
    {
        public static Agent Instance { get; private set; }
        public ClientConnectionHandler DispatcherConnections { get; private set; }
        public string Identifier { get; private set; }
        public string SignalrUrl { get; private set; }
        public string WebUrl { get; private set; }
        public AgentConfig Config { get; private set; }

        private dynamic ActiveDispatcher => activeDispatcher != null ? DispatcherConnections[activeDispatcher] : null;

        private HubConnection coordinator;
        private IHubContext dispatcherHubContext;
        private IDisposable host;
        private TaskExecutor taskExecutor;
        private List<string> dispatcherQueue = new List<string>();
        private string activeDispatcher;

        public Agent(AgentConfig config, TaskExecutor taskExecutor)
        {
            using (Trace.Log())
            {
                Instance = this;

                this.Identifier = $"{Constants.Names.Agent}_{Guid.NewGuid()}";
                this.SignalrUrl = $"127.0.0.1:{config.AgentPort}"; // TODO: get correct endpoint location
                this.WebUrl = $"127.0.0.1:{config.WebPort}"; // TODO: get correct endpoint location
                this.Config = config;

                this.taskExecutor = taskExecutor ?? throw new NullReferenceException("taskExecutor can't be null");
                this.taskExecutor.CompletedTask += CompleteTask;

                Console.WriteLine($"Identifier: {Identifier}");

                StartListeningForDispatchers();
                RegisterWithCoordinator();
            }
        }

        public void Dispose()
        {
            using (Trace.Log())
            {
                taskExecutor.CompletedTask -= CompleteTask;

                coordinator.Dispose();
                host.Dispose();
            }
        }

        private void StartListeningForDispatchers()
        {
            using (Trace.Log())
            {
                dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();

                DispatcherConnections = new ClientConnectionHandler(dispatcherHubContext);
                DispatcherConnections.EndpointAdded += AddDispatcher;
                DispatcherConnections.EndpointRemoved += RemoveDispatcher;

                var hostUrl = Permissions.GetHostUrl(Config.AgentPort);
                host = WebApp.Start(new StartOptions(hostUrl)
                {
                    AppStartup = typeof(AgentStartup).FullName
                });
                Console.WriteLine("Server running on {0}", hostUrl);
            }
        }

        private void RegisterWithCoordinator()
        {
            using (Trace.Log())
            {
                coordinator = new HubConnection($"http://{Config.CoordinatorAddress}", Identifier, SignalrUrl, WebUrl, "AgentHub");
                coordinator.Start();
            }
        }

        private void AddDispatcher(string name, string connectionInfo, EndpointConnectionInfo info)
        {
            using (Trace.Log())
            {
                lock (dispatcherQueue)
                {
                    if (!dispatcherQueue.Contains(name))
                    {
                        dispatcherQueue.Add(name);
                    }

                    if (activeDispatcher == null)
                    {
                        ActivateNextDispatcher();
                    }
                }
            }
        }

        private void RemoveDispatcher(string name)
        {
            using (Trace.Log())
            {
                lock (dispatcherQueue)
                {
                    dispatcherQueue.Remove(name);

                    if (activeDispatcher == name)
                    {
                        ActivateNextDispatcher();
                    }
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
            using (Trace.Log($"{activeDispatcher}: {ActiveDispatcher}"))
            {
                if (activeDispatcher != null)
                {
                    ActiveDispatcher.SetAgentState(Identifier, false);
                }

                lock (dispatcherQueue)
                {
                    if (dispatcherQueue.Count > 0)
                    {
                        activeDispatcher = dispatcherQueue[0];
                        dispatcherQueue.Remove(activeDispatcher);
                    }
                }

                if (activeDispatcher != null)
                {
                    ActiveDispatcher.SetAgentState(Identifier, true);
                }
            }
        }

        internal InitializationResult Initialize(object config)
        {
            using (Trace.Log())
            {
                var task = taskExecutor.Initialize(config);
                task.Wait();
                return task.Result;
            }
        }

        internal TaskResult[] StartTasks(IEnumerable<TaskItem> tasks)
        {
            using (Trace.Log())
            {
                var task = taskExecutor.StartTasks(tasks.ToArray());
                task.Wait();
                return task.Result;
            }
        }

        internal void CompleteTask(TaskItem task, TaskResult result)
        {
            using (Trace.Log())
            {
                ActiveDispatcher.CompleteTask(task, result);
            }
        }
    }
}
