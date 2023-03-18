using MessageBroker.Network.Client;
using System;

namespace MessageBroker.Network.Store.Base
{
    public interface IClientStore
    {
        IClient Get(Guid id);
        void Add(IClient client);
        void Remove(Guid id);
    }
}
