using Common;
using System;

namespace Dispatcher
{
    public class Config
    {
        public int priority;
    }

    public class Dispatcher : IDisposable
    {
        public string Identifier { get; private set; }
        public string EndpointData { get; private set; }
        public Config Config { get; private set; }

        private EndpointHubConnection coordinator;
        private AgentPool agents;

        public Dispatcher(Config config)
        {
            Identifier = $"{Constants.Names.Dispatcher}_{Guid.NewGuid()}";
            EndpointData = $"127.0.0.1:{Constants.Ports.DispatcherHost}";

            agents = new AgentPool(this);

            coordinator = new EndpointHubConnection($"http://localhost:{Constants.Ports.CoordinatorHost}/signalr", Identifier, EndpointData, "DispatcherHub");
            coordinator.EndpointAdded += agents.Add;
            coordinator.EndpointRemoved += agents.Remove;
            coordinator.EndpointListUpdated += agents.Update;
            coordinator.Start();
        }

        public void Dispose()
        {
            agents.Dispose();
            coordinator.Stop();
        }
    }
}
