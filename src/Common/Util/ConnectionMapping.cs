using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class ConnectionMapping
    {
        private readonly Dictionary<string, HashSet<string>> connections = new Dictionary<string, HashSet<string>>();

        public int Count => connections.Count;
        public IEnumerable<string> Keys => connections.Keys;
        public IHubContext HubContext { get; private set; }

        public ConnectionMapping(IHubContext hubContext)
        {
            HubContext = hubContext;
        }

        public bool Add(string key, string connectionId)
        {
            lock (connections)
            {
                bool added = false;
                if (!connections.TryGetValue(key, out var set))
                {
                    set = new HashSet<string>();
                    connections.Add(key, set);
                    added = true;
                }

                set.Add(connectionId);
                HubContext.Groups.Add(connectionId, key);

                return added;
            }
        }

        public dynamic Group(string key)
        {
            return HubContext.Clients.Group(key);
        }


        public IEnumerable<string> ConnectionIds(string key)
        {
            lock (connections)
            {
                if (connections.TryGetValue(key, out var set))
                {
                    return set;
                }

                return Enumerable.Empty<string>();
            }
        }

        public bool Remove(string key, string connectionId)
        {
            lock (connections)
            {
                if (!connections.TryGetValue(key, out var set))
                {
                    return false;
                }

                lock (set)
                {
                    set.Remove(connectionId);

                    if (set.Count == 0)
                    {
                        connections.Remove(key);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
