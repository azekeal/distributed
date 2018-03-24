using Distributed.Internal.Util;
using Distributed.Core;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Distributed.Internal.Server
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

        public void OnConnect(string name, string connectionId, string signalrUrl, string webUrl)
        {
            if (Connections.Add(name, connectionId))
            {
                var info = new EndpointConnectionInfo(name, signalrUrl, webUrl);
                lock (Endpoints)
                {
                    Endpoints[name] = info;
                }

                EndpointAdded?.Invoke(name, connectionId, info);
            }
        }

        public void OnDisconnect(string name, string connectionId)
        {
            EndpointRemoved?.Invoke(name);

            if (Connections.Remove(name, connectionId))
            {
                lock (Endpoints)
                {
                    Endpoints.Remove(name);
                }
            }
        }

        public void OnReconnect(string name, string connectionId, string signalrUrl, string webUrl)
        {
            if (!Connections.Contains(name, connectionId))
            {
                OnConnect(name, connectionId, signalrUrl, webUrl);
            }
        }
    }
}
