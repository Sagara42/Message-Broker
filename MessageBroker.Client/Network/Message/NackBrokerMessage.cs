using System;

namespace MessageBroker.Client.Network.Message
{
    public class NackBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.Nack.GetHashCode();
        public byte[] Payload { get; private set; }

        public Guid MessageNetIdentity { get; private set; }

        public NackBrokerMessage(Guid messageNetIdentity)
        {
            MessageNetIdentity = messageNetIdentity;

            SetupPayload();
        }

        public NackBrokerMessage(Guid netIdentity, byte[] payload)
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
