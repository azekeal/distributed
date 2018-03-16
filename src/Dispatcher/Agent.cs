using Common;
using Microsoft.AspNet.SignalR.Client;
using System;

namespace Dispatcher
{
    using HubConnection = Common.HubConnection;

    public class Agent : Endpoint
    {
        private HubConnection agentConnection;

        public Agent(string dispatcherId, string endpointData, EndpointConnectionInfo info) : base(info)
        {
            agentConnection = new HubConnection($"http://localhost:{Constants.Ports.AgentHost}/signalr", dispatcherId, endpointData, "DispatcherHub");
            agentConnection.Proxy.On<string>("AgentSaysHello", msg => Console.WriteLine(msg));
            // TODO: proxy callbacks
            agentConnection.Start();
        }

        public override void Dispose()
        {
            agentConnection.Dispose();
        }
    }
}
