using System;

namespace MessageBroker.Network.Message
{
    public class BrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.Message.GetHashCode();
        public byte[] Payload { get; private set; }

        public Guid SubscribeNetIdentity { get; private set; }
        public byte[] Data { get; set; }
        
        public BrokerMessage(Guid subscribeNetId, byte[] data)
        {
            SubscribeNetIdentity = subscribeNetId;
            Data = data;
            SetupPayload();
        }

        public void RestoreFromPayload()
        {
            throw new NotImplementedException();
        }

        private void SetupPayload() 
        {
            Payload = new byte[16 + Data.Length];
            Buffer.BlockCopy(SubscribeNetIdentity.ToByteArray(), 0, Payload, 0, 16);
            Buffer.BlockCopy(Data, 0, Payload, 16, Data.Length);
        }
    }
}
