using System;

namespace MessageBroker.Network.Message
{
    public class UnsubscribeBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.Unsubscribe.GetHashCode();
        public byte[] Payload { get; private set; }

        public Guid SubscribeNetIdentity { get; private set; }

        public UnsubscribeBrokerMessage(Guid subscribeNetIdentity)
        {
            SubscribeNetIdentity = subscribeNetIdentity;

            SetupPayload();
        }

        public UnsubscribeBrokerMessage(Guid netIdentity, byte[] payload)
        {
            NetIdentity = netIdentity;
            Payload = payload;
        }

        public void RestoreFromPayload()
        {
            SubscribeNetIdentity = new Guid(Payload);
        }

        private void SetupPayload()
        {
            Payload = SubscribeNetIdentity.ToByteArray();
        }
    }
}
