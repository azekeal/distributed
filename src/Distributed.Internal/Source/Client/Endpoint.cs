using Distributed.Core;
using System;

namespace Distributed.Internal.Client
{
    public class Endpoint : IDisposable
    {
        public EndpointConnectionInfo Info { get; private set; }

        public Endpoint(EndpointConnectionInfo info)
        {
            this.Info = info;
        }

        public string Name => Info.name;

        public virtual void Dispose()
        {
        }
    }
}
