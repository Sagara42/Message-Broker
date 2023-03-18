using System;
using System.Text;

namespace MessageBroker.Network.Message
{
    public class DeleteTopicBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; }

        public short OpCode => (short)OpCodes.DeleteTopic.GetHashCode();

        public byte[] Payload { get; private set; }

        public string Name { get; private set; }

        public DeleteTopicBrokerMessage(string name)
        {
            Name = name;
            SetupPayload();
        }

        public DeleteTopicBrokerMessage(Guid netIdentity, byte[] payload)
        {
            NetIdentity = netIdentity;
            Payload = payload;
        }

        public void RestoreFromPayload()
        {
            var name_length = BitConverter.ToInt32(Payload, 0);
            var name = Encoding.UTF8.GetString(Payload, 4, name_length);

            Name = name;
        }

        public void SetupPayload()
        {
            var name_array = Encoding.UTF8.GetBytes(Name);
            var name_len_array = BitConverter.GetBytes(name_array.Length);

            Payload = new byte[name_array.Length + 4];
            Buffer.BlockCopy(name_len_array, 0, Payload, 0, 4);
            Buffer.BlockCopy(name_array, 0, Payload, 4, name_array.Length);
        }
    }
}
