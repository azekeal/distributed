using Microsoft.AspNet.SignalR.Client;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Distributed.Internal.Client
{
    public class HubConnection : IDisposable
    {
        protected Microsoft.AspNet.SignalR.Client.HubConnection hubConnection;
        protected IHubProxy proxy;
        protected ConnectionState connectionState = ConnectionState.Disconnected;
        protected bool reconnect = true;
        protected string identifier;

        public event Action<StateChange> StateChanged;
        public string HubName { get; internal set; }
        public IHubProxy Proxy => proxy;

        public HubConnection(string url, string id, string endpointData, string hubName)
        {
            HubName = hubName;

            hubConnection = new Microsoft.AspNet.SignalR.Client.HubConnection(url);
            hubConnection.StateChanged += OnStateChanged;
            hubConnection.Credentials = CredentialCache.DefaultCredentials;
            hubConnection.Headers.Add("id", id);

            CreateProxy();
        }


        public virtual void Start()
        {
            Debug.Assert(connectionState == ConnectionState.Disconnected);
            OnStateChanged(new StateChange(ConnectionState.Disconnected, ConnectionState.Disconnected));
        }

        protected virtual void CreateProxy()
        {
            proxy = hubConnection.CreateHubProxy(HubName);
        }

        public void Stop()
        {
            hubConnection.Stop();
        }

        protected virtual void OnStateChanged(StateChange stateChange)
        {
            connectionState = stateChange.NewState;
            Console.WriteLine($"{stateChange.OldState} {stateChange.NewState}");

            if (stateChange.NewState == ConnectionState.Disconnected)
            {
                if (reconnect)
                {
                    Task.Delay(1000).ContinueWith((t) => hubConnection.Start());
                }
            }

            StateChanged?.Invoke(stateChange);
        }

        public virtual void Dispose()
        {
            reconnect = false;

            Stop();

            hubConnection?.Dispose();
            hubConnection = null;
        }
    }
}
