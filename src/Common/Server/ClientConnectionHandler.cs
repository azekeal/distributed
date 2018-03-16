using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class ClientConnectionHandler
    {
        public ConnectionMapping<string> ConnectionIds { get; private set; }
        public Dictionary<string, EndpointConnectionInfo> Endpoints { get; private set; }
        public event Action<string, string, EndpointConnectionInfo> EndpointAdded;
        public event Action<string> EndpointRemoved;

        public ClientConnectionHandler()
        {
            ConnectionIds = new ConnectionMapping<string>();
            Endpoints = new Dictionary<string, EndpointConnectionInfo>();
        }

        public void OnConnect(string name, string connectionId, string endpointData)
        {
            if (ConnectionIds.Add(name, connectionId))
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
            if (ConnectionIds.Remove(name, connectionId))
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
            if (!ConnectionIds.GetConnections(name).Contains(connectionId))
            {
                OnConnect(name, connectionId, endpointData);
            }
        }
    }
}
