using MessageBroker.Client.Network.Message;
using System;
using System.Linq;

namespace MessageBroker.Client.Network.Serialization
{
    public class BrokerSerializer : IBrokerSerializer
    {
        public T Deserialize<T>(byte[] data) where T : IMessage
        {
            var net_identity = new Guid(data.Skip(6).Take(16).ToArray());
            var payload = data.Skip(22).Take(data.Length - 22).ToArray();
            var instance = (T)Activator.CreateInstance(typeof(T), net_identity, payload);
            instance.RestoreFromPayload();

            return instance;
        }

        public byte[] Serialize(IMessage message)
        {
            var net_identity_array = message.NetIdentity.ToByteArray();
            var opcode_array = BitConverter.GetBytes(message.OpCode);
            var total_size = net_identity_array.Length + opcode_array.Length + message.Payload.Length;
            var total_size_array = BitConverter.GetBytes(total_size);

            var buffer = new byte[total_size + 4];

            Buffer.BlockCopy(total_size_array, 0, buffer, 0, 4);
            Buffer.BlockCopy(opcode_array, 0, buffer, 4, 2);
            Buffer.BlockCopy(net_identity_array, 0, buffer, 6, net_identity_array.Length);
            Buffer.BlockCopy(message.Payload, 0, buffer, 22, message.Payload.Length);

            return buffer;
        }
    }
}
