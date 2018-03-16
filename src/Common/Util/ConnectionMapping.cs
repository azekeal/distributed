using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class ConnectionMapping<T>
    {
        private readonly Dictionary<T, HashSet<string>> _connections = new Dictionary<T, HashSet<string>>();

        public int Count => _connections.Count;
        public IEnumerable<T> Keys => _connections.Keys;

        public bool Add(T key, string connectionId)
        {
            lock (_connections)
            {
                bool added = false;
                if (!_connections.TryGetValue(key, out var connections))
                {
                    connections = new HashSet<string>();
                    _connections.Add(key, connections);
                    added = true;
                }

                lock (connections)
                {
                    connections.Add(connectionId);
                }

                return added;
            }
        }

        public IEnumerable<string> this[T key]
        {
            get => GetConnections(key);
        }


        public IEnumerable<string> GetConnections(T key)
        {
            lock (_connections)
            {
                if (_connections.TryGetValue(key, out var connections))
                {
                    return connections;
                }

                return Enumerable.Empty<string>();
            }
        }

        public bool Remove(T key, string connectionId)
        {
            lock (_connections)
            {
                if (!_connections.TryGetValue(key, out var connections))
                {
                    return false;
                }

                lock (connections)
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(key);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
