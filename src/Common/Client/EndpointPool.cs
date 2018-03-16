using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Common
{

    public class EndpointPool : IDisposable
    {
        private object lockObj = new object();
        private Dictionary<string, Endpoint> endpoints = new Dictionary<string, Endpoint>();

        public EndpointPool()
        {
        }

        public virtual Endpoint CreateEndpoint(EndpointConnectionInfo info)
        {
            return new Endpoint(info);
        }

        public void Add(EndpointConnectionInfo info)
        {
            lock (lockObj)
            {
                Debug.Assert(!endpoints.ContainsKey(info.name));
                endpoints.Add(info.name, CreateEndpoint(info));
            }
        }

        public void Remove(string name)
        {
            lock (lockObj)
            {
                if (endpoints.TryGetValue(name, out var endpoint))
                {
                    endpoints.Remove(name);
                    endpoint.Dispose();
                }
            }
        }

        public void Clear()
        {
            lock (lockObj)
            {
                foreach (var endpoint in endpoints.Values)
                {
                    endpoint.Dispose();
                }

                endpoints.Clear();
            }
        }

        public void Update(IEnumerable<EndpointConnectionInfo> list)
        {
            lock (lockObj)
            {
                var add = new List<EndpointConnectionInfo>();
                var marked = new HashSet<string>();
                var remove = new List<string>();

                foreach (var item in list)
                {
                    if (!endpoints.ContainsKey(item.name))
                    {
                        add.Add(item);
                    }
                    else
                    {
                        marked.Add(item.name);
                    }
                }

                foreach (var name in endpoints.Keys)
                {
                    if (!marked.Contains(name))
                    {
                        remove.Add(name);
                    }
                }

                foreach (var info in add)
                {
                    Add(info);
                }

                foreach (var name in remove)
                {
                    Remove(name);
                }
            }
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
