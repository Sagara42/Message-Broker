using System;
using System.Text;

namespace MessageBroker.Client.Network.Message
{
    internal class ResponseBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();

        public short OpCode => (short)OpCodes.Response.GetHashCode();

        public byte[] Payload { get; private set; }

        public string ExceptionMessage { get; private set; }
        public ResponseType ResponseType { get; private set; }
        public Guid OnMessageResponseId { get; private set; }

        public ResponseBrokerMessage(Guid messageId, ResponseType type, string message = "")
        {
            OnMessageResponseId = messageId;
            ResponseType = type;
            ExceptionMessage = message;
            SetupPayload();
        }

        public ResponseBrokerMessage(Guid netId, byte[] payload)
        {
            NetIdentity = netId;
            Payload = payload;
        }

        public void RestoreFromPayload()
        {
            ResponseType = (ResponseType)Payload[0];
            var message_net_id = new byte[16];
            Buffer.BlockCopy(Payload, 1, message_net_id, 0, 16);
            OnMessageResponseId = new Guid(message_net_id);

            var message_len = BitConverter.ToInt32(Payload, 17);
            if (message_len > 0)
            {
                var message = Encoding.UTF8.GetString(Payload, 5 + 16, message_len);
                ExceptionMessage = message;
            }
        }

        private void SetupPayload()
        {
            var message_array = Encoding.UTF8.GetBytes(ExceptionMessage);
            var message_array_len = BitConverter.GetBytes(message_array.Length);

            Payload = new byte[message_array.Length + 5 + 16];
            Payload[0] = (byte)ResponseType.GetHashCode();
            Buffer.BlockCopy(OnMessageResponseId.ToByteArray(), 0, Payload, 1, 16);

            Buffer.BlockCopy(message_array_len, 0, Payload, 17, 4);
            Buffer.BlockCopy(message_array, 0, Payload, 5 + 16, message_array.Length);
        }
    }

    public enum ResponseType : byte
    {
        Success = 0x00,
        Exception = 0xFF
    }
}
