using MessageBroker.Network.Message;
using System;

namespace MessageBroker.ClientCommunication
{
    internal interface IClientCommunication
    {
        void CheckMessages();
        void HandleMessage(Guid clientId, BrokerMessage message);
        void Ack(Guid messageId);
        void Nack(Guid messageId);
    }
}
