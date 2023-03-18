using MessageBroker.Network.Message;
using System;
using System.Net.Sockets;

namespace MessageBroker.Network.Client
{
    public interface IClient
    {
        Guid NetIdentity { get; }
        Socket Socket { get; }
        void SendMessage(IMessage message);
    }
}
