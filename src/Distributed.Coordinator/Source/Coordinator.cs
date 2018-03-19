﻿using Distributed.Core;
using Distributed.Internal;
using Distributed.Internal.Server;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;

namespace Distributed
{
    public class CoordinatorConfig
    {
        public int CoordinatorPort = Constants.Ports.CoordinatorHost;
    }

    public sealed class Coordinator : IDisposable
    {
        public static Coordinator Instance { get;private set; }
        public ClientConnectionHandler DispatcherConnections { get; private set; }
        public ClientConnectionHandler AgentConnections { get; private set; }
        public CoordinatorConfig Config { get; private set; }

        private IHubContext dispatcherHubContext;
        private IHubContext agentHubContext;
        private IDisposable host;

        public Coordinator() : this(new CoordinatorConfig()) { }
        public Coordinator(CoordinatorConfig config)
        {
            Instance = this;
            Config = config;

            StartListeningForDispatchersAndAgents();
        }

        private void StartListeningForDispatchersAndAgents()
        {
            dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();
            agentHubContext = GlobalHost.ConnectionManager.GetHubContext<AgentHub>();

            DispatcherConnections = new ClientConnectionHandler(dispatcherHubContext);
            DispatcherConnections.EndpointAdded += OnDispatcherAdded;

            AgentConnections = new ClientConnectionHandler(agentHubContext);
            AgentConnections.EndpointAdded += OnAgentAdded;
            AgentConnections.EndpointRemoved += OnAgentRemoved;

            host = WebApp.Start(new StartOptions(HostUrl)
            {
                AppStartup = typeof(CoordinatorStartup).FullName
            });
            Console.WriteLine("Server running on {0}", HostUrl);
        }

        public string HostUrl => $"http://localhost:{Config.CoordinatorPort}";

        public void Dispose()
        {
            host.Dispose();
        }

        private void OnDispatcherAdded(string name, string connectionId, EndpointConnectionInfo info)
        {
            // Notify new dispatcher of available agents
            // TODO: segment agents/dispatchers by subnet
            dispatcherHubContext.Clients.Client(connectionId).EndpointListUpdated(AgentConnections.Endpoints.Values);
        }

        private void OnAgentAdded(string name, string endpointData, EndpointConnectionInfo info)
        {
            // Notify known dispatchers of new agent
            dispatcherHubContext.Clients.All.EndpointAdded(info);
        }

        private void OnAgentRemoved(string name)
        {
            // Notify known dispatchers agent has died
            dispatcherHubContext.Clients.All.EndpointRemoved(name);
        }
    }
}