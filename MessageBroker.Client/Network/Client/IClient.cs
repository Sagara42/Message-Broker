using MessageBroker.Client.Network.Message;
using System;
using System.Net.Sockets;

namespace MessageBroker.Client.Network.Client
{
    public interface IClient
    {
        Socket Socket { get; }
        void Connect();
        void Disconnect();
        void Send(IMessage message);
        Subscription Subscribe(string topic_name);
        void Unsubscribe(Guid topic_id);
        void Publish(string path, byte[] data);
        void DeclareTopic(string name, string path);
        void DeleteTopic(string name);
    }
}
