using Distributed.Core;
using Distributed.Monitor;
using Distributed.Internal.Server;
using Microsoft.AspNet.SignalR;
using System;
using System.Linq;
using System.IO;
using Distributed.Internal.Util;

namespace Distributed.Internal
{
    public class CoordinatorMonitor : IDisposable
    {
        private ClientConnectionHandler connections;
        private IHubContext context;
        private Coordinator coordinator;
        private WebFileServer webFileServer;

        public CoordinatorMonitor(Coordinator coordinator)
        {
            this.coordinator = coordinator;

            context = GlobalHost.ConnectionManager.GetHubContext<MonitorHub>();
            connections = new ClientConnectionHandler(context);

            coordinator.DispatcherConnections.EndpointAdded += OnDispatcherAdded;
            coordinator.DispatcherConnections.EndpointRemoved += OnDispatcherRemoved;
            coordinator.AgentConnections.EndpointAdded += OnAgentAdded;
            coordinator.AgentConnections.EndpointRemoved += OnAgentRemoved;

            var hostUrl = Permissions.GetHostUrl(Constants.Ports.CoordinatorWebHost);
            webFileServer = new WebFileServer(hostUrl, Path.Combine(Environment.CurrentDirectory, @"Content\Coordinator"));
        }

        private void OnAgentRemoved(string obj)
        {
            context.Clients.All.setAgents(Coordinator.Instance.AgentConnections.Endpoints.Values.ToArray());
        }

        private void OnAgentAdded(string arg1, string arg2, EndpointConnectionInfo arg3)
        {
            context.Clients.All.setAgents(Coordinator.Instance.AgentConnections.Endpoints.Values.ToArray());
        }

        private void OnDispatcherRemoved(string obj)
        {
            context.Clients.All.setDispatchers(Coordinator.Instance.DispatcherConnections.Endpoints.Values.ToArray());
        }

        private void OnDispatcherAdded(string arg1, string arg2, Core.EndpointConnectionInfo arg3)
        {
            context.Clients.All.setDispatchers(Coordinator.Instance.DispatcherConnections.Endpoints.Values.ToArray());
        }

        public void Dispose()
        {
            webFileServer.Dispose();

            coordinator.DispatcherConnections.EndpointAdded -= OnDispatcherAdded;
            coordinator.DispatcherConnections.EndpointRemoved -= OnDispatcherRemoved;
            coordinator.AgentConnections.EndpointAdded -= OnAgentAdded;
            coordinator.AgentConnections.EndpointRemoved -= OnAgentRemoved;
        }
    }

}
