using System;

namespace MessageBroker.Network.Message
{
    public class AckBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.Ack.GetHashCode();
        public byte[] Payload { get; private set; }

        public Guid MessageNetIdentity { get; private set; }

        public AckBrokerMessage(Guid messageNetIdentity)
        {
            MessageNetIdentity = messageNetIdentity;

            SetupPayload();
        }

        public AckBrokerMessage(Guid netIdentity, byte[] payload)
        {
            NetIdentity = netIdentity;
            Payload = payload;
        }

        public void RestoreFromPayload()
        {
            MessageNetIdentity = new Guid(Payload);
        }

        private void SetupPayload()
        {
            Payload = MessageNetIdentity.ToByteArray();
        }
    }
}
