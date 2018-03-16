using Common;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;

namespace Coordinator
{
    public class AgentHub : EndpointHub
    {
        public AgentHub() => ClientConnectionHandler = Coordinator.Instance.AgentConnections;
    }

    public class DispatcherHub : EndpointHub
    {
        public DispatcherHub() => ClientConnectionHandler = Coordinator.Instance.DispatcherConnections;
    }

    public class Coordinator : IDisposable
    {
        public static Coordinator Instance { get;private set; }

        public ClientConnectionHandler DispatcherConnections;
        public ClientConnectionHandler AgentConnections;

        private IHubContext dispatcherHubContext;
        private IHubContext agentHubContext;
        private IDisposable host;

        public Coordinator()
        {
            Instance = this;

            DispatcherConnections = new ClientConnectionHandler();
            DispatcherConnections.EndpointAdded += OnDispatcherAdded;

            AgentConnections = new ClientConnectionHandler();
            AgentConnections.EndpointAdded += OnAgentAdded;
            AgentConnections.EndpointRemoved += OnAgentRemoved;

            dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();
            agentHubContext = GlobalHost.ConnectionManager.GetHubContext<AgentHub>();

            host = WebApp.Start(HostUrl);
            Console.WriteLine("Server running on {0}", HostUrl);
        }

        public string HostUrl => $"http://localhost:{Constants.Ports.CoordinatorHost}";

        private void OnDispatcherAdded(string name, string connectionId, EndpointConnectionInfo info)
        {
            // Notify new dispatcher of available agents
            // TODO: segment agents/dispatchers by subnet
            dispatcherHubContext.Clients.Client(connectionId).EndpointListUpdated(AgentConnections.Endpoints.Values);
        }

        internal void OnAgentAdded(string name, string endpointData, EndpointConnectionInfo info)
        {
            // Notify known dispatchers of new agent
            dispatcherHubContext.Clients.All.EndpointAdded(info);
        }

        internal void OnAgentRemoved(string name)
        {
            // Notify known dispatchers agent has died
            dispatcherHubContext.Clients.All.EndpointRemoved(name);
        }

        public void Dispose()
        {
            host.Dispose();
        }
    }
}
