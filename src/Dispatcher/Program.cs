using Messages;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Dispatcher
{
    class Program
    {
        private static IHubProxy proxy;
        private static HubConnection hubConnection;
        private static ConnectionState connectionState;
        private static ConnectionMapping<string> agents = new ConnectionMapping<string>();

        private static async Task MainAsync()
        {
            hubConnection = new HubConnection("http://localhost:8080/signalr");
            hubConnection.StateChanged += HubConnection_StateChanged;
            //hubConnection.Credentials = CredentialCache.DefaultCredentials;

            proxy = hubConnection.CreateHubProxy("DispatcherHub");
            proxy.On("AgentAdded", agent => Console.WriteLine($"agent added"));
            proxy.On("AgentRemoved", agent => Console.WriteLine($"agent removed"));
            proxy.On("AgentListUpdated", list => Console.WriteLine($"agent list updated"));


            while (true)
            {
                await hubConnection.Start();
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line.Length == 0)
                    {
                        hubConnection.Stop();
                        break;
                    }
                    else
                    {
                        await proxy.Invoke("SendChatMessage", "me", line);
                    }
                }
            }
        }

        private static void HubConnection_StateChanged(StateChange obj)
        {
            connectionState = obj.NewState;
            Console.WriteLine($"{obj.OldState} {obj.NewState}");

            if (obj.NewState == ConnectionState.Connected)
            {
                // ask for agent list
            }
        }

        static void Main(string[] args)
        {
            Task.Run(async () => await MainAsync()).Wait();
        }
    }
}
