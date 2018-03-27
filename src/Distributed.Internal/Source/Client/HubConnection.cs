using Microsoft.AspNet.SignalR.Client;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Distributed.Internal.Client
{
    public class HubConnection : IDisposable
    {
        // Id for this side of the connection (e.g. dispatcherId)
        public string Identifier { get; private set; }

        protected Microsoft.AspNet.SignalR.Client.HubConnection hubConnection;
        protected IHubProxy proxy;
        protected ConnectionState connectionState = ConnectionState.Disconnected;
        protected bool reconnect = true;

        public event Action<StateChange> StateChanged;
        public string HubName { get; internal set; }
        public IHubProxy Proxy => proxy;

        public HubConnection(string url, string id, string signalrUrl, string webUrl, string hubName)
        {
            using (Trace.Log())
            {

                HubName = hubName;

                Identifier = id;

                hubConnection = new Microsoft.AspNet.SignalR.Client.HubConnection(url);
                hubConnection.StateChanged += OnStateChanged;
                hubConnection.Credentials = CredentialCache.DefaultCredentials;
                hubConnection.Headers.Add("id", id);
                hubConnection.Headers.Add("signalrUrl", signalrUrl);
                hubConnection.Headers.Add("webUrl", webUrl);

                CreateProxy();
            }
        }


        public virtual void Start()
        {
            using (Trace.Log())
            {
                Debug.Assert(connectionState == ConnectionState.Disconnected);
                OnStateChanged(new StateChange(ConnectionState.Disconnected, ConnectionState.Disconnected));
            }
        }

        protected virtual void CreateProxy()
        {
            using (Trace.Log())
            {
                proxy = hubConnection.CreateHubProxy(HubName);
            }
        }

        public void Stop()
        {
            using (Trace.Log())
            {
                hubConnection?.Stop();
            }
        }

        protected virtual void OnStateChanged(StateChange stateChange)
        {
            using (Trace.Log())
            {
                connectionState = stateChange.NewState;
                Console.WriteLine($"{stateChange.OldState} {stateChange.NewState}");

                if (stateChange.NewState == ConnectionState.Disconnected)
                {
                    if (stateChange.OldState == ConnectionState.Disconnected)
                    {
                        hubConnection?.Start();
                    }
                    else if (reconnect)
                    {
                        Task.Delay(1000).ContinueWith((t) => hubConnection?.Start());
                    }
                }

                StateChanged?.Invoke(stateChange);
            }
        }

        public virtual void Dispose()
        {
            using (Trace.Log())
            {
                reconnect = false;

                //Stop();

                hubConnection.StateChanged -= OnStateChanged;
                hubConnection.Dispose();
                hubConnection = null;
            }
        }
    }
}
