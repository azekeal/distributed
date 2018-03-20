﻿using Distributed.Core;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;

namespace Distributed.Internal.Client
{
    public class EndpointHubConnection : HubConnection
    {
        public EndpointHubConnection(string url, string id, string signalrUrl, string webUrl, string hubName) : base(url, id, signalrUrl, webUrl, hubName)
        {
        }

        public event Action<EndpointConnectionInfo> EndpointAdded;
        public event Action<string> EndpointRemoved;
        public event Action<IEnumerable<EndpointConnectionInfo>> EndpointListUpdated;
        
        protected override void CreateProxy()
        {
            base.CreateProxy();

            proxy.On<EndpointConnectionInfo>("EndpointAdded", info =>
            {
                Console.WriteLine($"Endpoint added: {info}");
                EndpointAdded?.Invoke(info);
            });

            proxy.On<string>("EndpointRemoved", name =>
            {
                Console.WriteLine($"Endpoint removed: {name}");
                EndpointRemoved?.Invoke(name);
            });

            proxy.On<IEnumerable<EndpointConnectionInfo>>("EndpointListUpdated", infos =>
            {
                Console.WriteLine($"Endpoint list updated: {string.Join(", ", infos)}");
                EndpointListUpdated?.Invoke(infos);
            });

            base.CreateProxy();
        }
    }
}
