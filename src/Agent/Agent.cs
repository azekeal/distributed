using Common;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using System;

namespace Agent
{
    public class DispatcherHub : EndpointHub
    {
        public DispatcherHub() => ClientConnectionHandler = Agent.Instance.DispatcherConnections;
    }

    public class Agent : IDisposable
    {
        public static Agent Instance { get; private set; }
        public ClientConnectionHandler DispatcherConnections { get; private set; }
        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }

        private HubConnection coordinator;
        private IHubContext dispatcherHubContext;
        private IDisposable host;

        public Agent()
        {
            Instance = this;
            Identifier = $"{Constants.Names.Agent}_{Guid.NewGuid()}";
            EndpointData = $"127.0.0.1:{Constants.Ports.AgentHost}";

            StartListeningForDispatchers();
            RegisterWithCoordinator();
        }

        private void StartListeningForDispatchers()
        {
            dispatcherHubContext = GlobalHost.ConnectionManager.GetHubContext<DispatcherHub>();

            DispatcherConnections = new ClientConnectionHandler();
            DispatcherConnections.EndpointAdded += (name, connectionId, info) => 
            {
                foreach (var c in DispatcherConnections.ConnectionIds[name])
                {
                    dispatcherHubContext.Clients.Client(c).AgentSaysHello("hello");
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

        public string HostUrl => $"http://localhost:{Constants.Ports.AgentHost}";

        public void Dispose()
        {
            coordinator.Dispose();
            host.Dispose();
        }
    }    
}
