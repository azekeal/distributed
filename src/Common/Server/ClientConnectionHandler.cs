using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class ClientConnectionHandler
    {
        public ConnectionMapping Connections { get; private set; }
        public Dictionary<string, EndpointConnectionInfo> Endpoints { get; private set; }
        public event Action<string, string, EndpointConnectionInfo> EndpointAdded;
        public event Action<string> EndpointRemoved;
        public IHubContext HubContext => Connections.HubContext;

        public ClientConnectionHandler(IHubContext hubContext)
        {
            Connections = new ConnectionMapping(hubContext);
            Endpoints = new Dictionary<string, EndpointConnectionInfo>();
        }

        public dynamic this[string name] => Connections.Group(name);

        public void OnConnect(string name, string connectionId, string endpointData)
        {
            if (Connections.Add(name, connectionId))
            {
                var info = new EndpointConnectionInfo(name, endpointData);
                lock (Endpoints)
                {
                    Endpoints[name] = info;
                }

                EndpointAdded?.Invoke(name, connectionId, info);
            }
        }

        public void OnDisconnect(string name, string connectionId)
        {
            if (Connections.Remove(name, connectionId))
            {
                lock (Endpoints)
                {
                    Endpoints.Remove(name);
                }

                EndpointRemoved?.Invoke(name);
            }
        }

        public void OnReconnect(string name, string connectionId, string endpointData)
        {
            if (!Connections.ConnectionIds(name).Contains(connectionId))
            {
                OnConnect(name, connectionId, endpointData);
            }
        }
    }
}
