using MessageBroker.Client.Network.Client;
using MessageBroker.Client.Network.Message;
using System;

namespace MessageBroker.Client
{
    public class Subscription
    {
        public Guid Id { get; private set; }
        public string TopicName { get; private set; }

        public Action<PublishData> OnMessageReceived;

        public Subscription(string topic_name)
        {
            Id = Guid.NewGuid();
            TopicName = topic_name;
        }
    }

    public class PublishData
    {
        public byte[] Data { get; private set; }

        private IClient _client;
        private Guid _messageId;

        public PublishData(IClient client, Guid messageId, byte[] data)
        {
            _client = client;
            _messageId = messageId;

            Data = data;
        }

        public void Ack()
        {
            _client.Send(new AckBrokerMessage(_messageId));
        }
    }
}
