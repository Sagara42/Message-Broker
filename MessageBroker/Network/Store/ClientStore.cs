using MessageBroker.Network.Client;
using MessageBroker.Network.Store.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MessageBroker.Network.Store
{
    internal class ClientStore : IClientStore
    {
        private ConcurrentDictionary<Guid, IClient> _clients;
        public ClientStore()
        {
            _clients = new();
        }

        public void Add(IClient client)
        {
            _ = _clients.TryAdd(client.NetIdentity, client);
        }

        public IClient Get(Guid id)
        {
            return _clients.Values.FirstOrDefault(s => s.NetIdentity == id);
        }

        public IEnumerable<IClient> GetClients()
        {
            return _clients.Values;
        }

        public void Remove(Guid id)
        {
            _ = _clients.TryRemove(id, out _);
        }
    }
}
