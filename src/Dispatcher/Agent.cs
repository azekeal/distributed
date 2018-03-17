using Common;
using Microsoft.AspNet.SignalR.Client;
using System;

namespace Dispatcher
{
    using HubConnection = Common.HubConnection;

    public class Agent : Endpoint
    {
        private Dispatcher dispatcher;
        private HubConnection agentConnection;
        private string agentId;

        public Agent(Dispatcher dispatcher, EndpointConnectionInfo info) : base(info)
        {
            this.dispatcher = dispatcher;

            agentConnection = new HubConnection($"http://localhost:{Constants.Ports.AgentHost}/signalr", dispatcher.Identifier, dispatcher.EndpointData, "DispatcherHub");
            agentConnection.StateChanged += OnStateChanged;
            agentConnection.Proxy.On<string>("SetAgentIdentifier", id =>
            {
                agentId = id;
                Console.WriteLine($"Connected to agent {agentId}");
            });

            agentConnection.Proxy.On("GetTaskPriorities", () =>
            {
                Console.WriteLine("GetTaskPriorities");
            });
            agentConnection.Start();
        }

        private void OnStateChanged(StateChange stateChange)
        {
            if (stateChange.NewState == ConnectionState.Connected)
            {
                agentConnection.Proxy.Invoke("SetDispatcherPriority", dispatcher.Config.priority);
            }
        }

        public override void Dispose()
        {
            agentConnection.Dispose();
        }
    }
}
