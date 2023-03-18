using System;

namespace MessageBroker.Client.Network.Message
{
    public interface IMessage
    {
        Guid NetIdentity { get; }
        short OpCode { get; }
        byte[] Payload { get; }

        void RestoreFromPayload();
    }
}
