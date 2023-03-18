using System;
using System.Text;

namespace MessageBroker.Client.Network.Message
{
    public class SubscribeBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.Subscribe.GetHashCode();
        public byte[] Payload { get; private set; }

        public Guid SubscribeNetIdentity { get; private set; }
        public string TopicName { get; private set; }

        public SubscribeBrokerMessage(Guid subscribeNetIdentity, string topicName)
        {
            SubscribeNetIdentity = subscribeNetIdentity;
            TopicName = topicName;

            SetupPayload();        
        }

        public SubscribeBrokerMessage(Guid netIdentity, byte[] payload)
        {
            NetIdentity = netIdentity;
            Payload = payload;
        }

        public void RestoreFromPayload()
        {
            var net_identity_array = new byte[16];
            Buffer.BlockCopy(Payload, 0, net_identity_array, 0, 16);
            SubscribeNetIdentity = new Guid(net_identity_array);

            var name_len = BitConverter.ToInt32(Payload, 16);
            var name_array = new byte[name_len];
            Buffer.BlockCopy(Payload, 20, name_array, 0, name_len);
            TopicName = Encoding.UTF8.GetString(name_array);
        }

        private void SetupPayload()
        {
            var topic_name_array = Encoding.UTF8.GetBytes(TopicName);
            var topic_name_len_array = BitConverter.GetBytes(topic_name_array.Length);

            Payload = new byte[20 + topic_name_array.Length];
            Buffer.BlockCopy(SubscribeNetIdentity.ToByteArray(), 0, Payload, 0, 16);
            Buffer.BlockCopy(topic_name_len_array, 0, Payload, 16, 4);
            Buffer.BlockCopy(topic_name_array, 0, Payload, 20, topic_name_array.Length);
        }
    }
}
