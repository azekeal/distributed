using Distributed.Core;
using Distributed.Internal;
using Distributed.Internal.Server;
using Distributed.Internal.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;

namespace Distributed
{
    public class CoordinatorConfig
    {
        public int CoordinatorPort = Constants.Ports.CoordinatorHost;
        public bool Monitor = true;
    }

    public sealed class Coordinator : IDisposable
    {
        public static Coordinator Instance { get;private set; }
        public ClientConnectionHandler DispatcherConnections { get; private set; }

        public ClientConnectionHandler AgentConnections { get; private set; }
        public CoordinatorConfig Config { get; private set; }

        private AgentAllocator agentAllocator;
        private IHubContext dispatcherHubContext;
        private IHubContext agentHubContext;
        private IDisposable host;
        private IDisposable monitor;

        public Coordinator() : this(new CoordinatorConfig()) { }
        public Coordinator(CoordinatorConfig config)
        {
            using (Trace.Log())
            {
                Instance = this;
                Config = config;

                agentAllocator = new AgentAllocator();
                agentAllocator.DispatcherAssignAgent += OnDispatcherAssignAgent;
                agentAllocator.DispatcherRemoveAgent += OnDispatcherRemoveAgent;

                StartListeningForDispatchersAndAgents();

                if (config.Monitor)
                {
                    monitor = new CoordinatorMonitor(this);
                }
            }
        }

        private void OnDispatcherRemoveAgent(string dispatcherId, EndpointConnectionInfo agentInfo)
        {
            using (Trace.Log())
            {
                DispatcherConnections[dispatcherId].EndpointRemoved(agentInfo.name);
            }
        }

        private void OnDispatcherAssignAgent(string dispatcherId, EndpointConnectionInfo agentInfo)
        {
            var connection = DispatcherConnections[dispatcherId];
            using (Trace.Log($"{connection}[{dispatcherId}].EndpointAdded({agentInfo})"))
            {
                connection.EndpointAdded(agentInfo);
            }
        }

        private void StartListeningForDispatchersAndAgents()
        {
            dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();
            agentHubContext = GlobalHost.ConnectionManager.GetHubContext<AgentHub>();

            DispatcherConnections = new ClientConnectionHandler(dispatcherHubContext);
            DispatcherConnections.EndpointAdded += OnDispatcherAdded;
            DispatcherConnections.EndpointRemoved += OnDispatcherRemoved;

            AgentConnections = new ClientConnectionHandler(agentHubContext);
            AgentConnections.EndpointAdded += OnAgentAdded;
            AgentConnections.EndpointRemoved += OnAgentRemoved;

            var hostUrl = Permissions.GetHostUrl(Config.CoordinatorPort);
            host = WebApp.Start(new StartOptions(hostUrl)
            {
                AppStartup = typeof(CoordinatorStartup).FullName
            });
            Console.WriteLine("Server running on {0}", hostUrl);
        }

        public void Dispose()
        {
            using (Trace.Log())
            {
                agentAllocator.Dispose();

                host.Dispose();

                monitor?.Dispose();
                monitor = null;
            }
        }

        private void OnDispatcherAdded(string dispatcherId, string connectionId, EndpointConnectionInfo info)
        {
            using (Trace.Log())
            {
                agentAllocator.AddDispatcher(dispatcherId, info);
            }
        }

        private void OnDispatcherRemoved(string dispatcherId)
        {
            using (Trace.Log())
            {
                agentAllocator.RemoveDispatcher(dispatcherId);
            }
        }

        private void OnAgentAdded(string agentId, string endpointData, EndpointConnectionInfo info)
        {
            using (Trace.Log())
            {
                agentAllocator.AddAgent(agentId, info);
            }
        }

        private void OnAgentRemoved(string agentId)
        {
            using (Trace.Log())
            {
                agentAllocator.RemoveAgent(agentId);
            }
        }

        internal void UpdateJob(string dispatcherId, string jobId, int jobPriority, int jobTaskCount)
        {
            using (Trace.Log())
            {
                agentAllocator.UpdateJob(dispatcherId, jobId, jobPriority, jobTaskCount);
            }
        }

        internal void ClearJob(string dispatcherId)
        {
            using (Trace.Log())
            {
                agentAllocator.ClearJob(dispatcherId);
            }
        }

        internal void ReleaseAgent(string dispatcherId, string jobId, string agentId)
        {
            using (Trace.Log())
            {
                agentAllocator.ReleaseAgent(dispatcherId, jobId, agentId);
            }
        }
    }
}
