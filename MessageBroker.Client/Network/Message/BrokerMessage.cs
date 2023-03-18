using System;

namespace MessageBroker.Client.Network.Message
{
    public class BrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.Message.GetHashCode();
        public byte[] Payload { get; private set; }

        public Guid SubscribeNetIdentity { get; private set; }
        public byte[] Data { get; set; }
        
        public BrokerMessage(Guid netId, byte[] data)
        {
            NetIdentity = netId;
            Payload = data;
        }

        public void RestoreFromPayload()
        {
            var guid_array = new byte[16];
            Buffer.BlockCopy(Payload, 0, guid_array, 0, 16);

            SubscribeNetIdentity = new Guid(guid_array);
            Data = new byte[Payload.Length - 16];
            Buffer.BlockCopy(Payload, 16, Data, 0, Payload.Length - 16);
        }
    }
}
