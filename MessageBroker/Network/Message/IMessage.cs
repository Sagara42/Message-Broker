using System;

namespace MessageBroker.Network.Message
{
    public interface IMessage
    {
        Guid NetIdentity { get; }
        short OpCode { get; }
        byte[] Payload { get; }

        void RestoreFromPayload();
    }
}
