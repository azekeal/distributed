using Microsoft.AspNet.SignalR;
using System.Collections.Generic;

namespace Distributed.Internal.Util
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

        public bool Contains(string key, string connectionId)
        {
            lock (connections)
            {
                return connections.TryGetValue(key, out var set) && set.Contains(connectionId);
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

                set.Remove(connectionId);

                if (set.Count == 0)
                {
                    connections.Remove(key);
                    return true;
                }
            }

            return false;
        }
    }
}
