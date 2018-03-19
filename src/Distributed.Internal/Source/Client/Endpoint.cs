using Distributed.Core;
using System;

namespace Distributed.Internal.Client
{
    public class Endpoint : IDisposable
    {
        public EndpointConnectionInfo info;

        public Endpoint(EndpointConnectionInfo info)
        {
            this.info = info;
        }

        public string Name => info.name;

        public virtual void Dispose()
        {
        }
    }
}
