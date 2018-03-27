using Distributed.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Distributed.Internal.Client
{

    public class EndpointPool : IDisposable
    {
        protected ConcurrentDictionary<string, Endpoint> endpoints = new ConcurrentDictionary<string, Endpoint>();
        private object lockObj = new object();

        public int Count => endpoints.Count;

        public EndpointPool()
        {
            using (Trace.Log())
            {
            }
        }

        public virtual Endpoint CreateEndpoint(EndpointConnectionInfo info)
        {
            using (Trace.Log($"{info}"))
            { 
                return new Endpoint(info);
            }
        }

        public void Add(EndpointConnectionInfo info)
        {
            using (Trace.Log($"{info}"))
            {
                lock (lockObj)
                {
                    if (!endpoints.ContainsKey(info.name))
                    {
                        foreach (var v in endpoints.Values)
                        {
                            // check for rebooted agents on the same ip/port
                            if (v.Info.signalrUrl == info.signalrUrl)
                            {
                                Remove(v.Name);
                                break; ;
                            }
                        }

                        endpoints.TryAdd(info.name, CreateEndpoint(info));
                    }
                }
            }
        }

        public void Remove(string name)
        {
            using (Trace.Log($"{name}"))
            {
                lock (lockObj)
                {
                    if (endpoints.TryRemove(name, out var endpoint))
                    {
                        endpoint.Dispose();
                    }
                }
            }
        }

        public void Clear()
        {
            using (Trace.Log())
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
        }

        public void Update(IEnumerable<EndpointConnectionInfo> list)
        {
            using (Trace.Log($"{string.Join(", ", list.Select(e => e.ToString()))}"))
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
        }

        public void Dispose()
        {
            using (Trace.Log())
            {
                Clear();
            }
        }
    }
}
