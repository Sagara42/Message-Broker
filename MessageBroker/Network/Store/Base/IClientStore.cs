using MessageBroker.Network.Client;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MessageBroker.Network.Store.Base
{
    public interface IClientStore
    {
        IEnumerable<IClient> GetClients();
        IClient Get(Guid id);
        void Add(IClient client);
        void Remove(Guid id);
    }
}
