using System;
using System.Text;

namespace MessageBroker.Network.Message
{
    public class PublishBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.Publish.GetHashCode();
        public byte[] Payload { get; private set; }

        public string Path { get; private set; }
        public byte[] Data { get; set; }

        public PublishBrokerMessage(string path, byte[] data)
        {
            Path = path;
            Data = data;

            SetupPayload();
        }

        public PublishBrokerMessage(Guid netIdentity, byte[] payload)
        {
            NetIdentity = netIdentity;
            Payload = payload;
        }

        public void RestoreFromPayload()
        {
            var path_len = BitConverter.ToInt32(Payload, 0);
            Path = Encoding.UTF8.GetString(Payload, 4, path_len);

            var data_len = BitConverter.ToInt32(Payload, 4 + path_len);
            Data = new byte[data_len];
            Buffer.BlockCopy(Payload, 8 + path_len, Data, 0, data_len);
        }

        private void SetupPayload()
        {
            var path_array = Encoding.UTF8.GetBytes(Path);
            var path_len_array = BitConverter.GetBytes(path_array.Length);
            var data_len_array = BitConverter.GetBytes(Data.Length);

            Payload = new byte[path_array.Length + 8 + Data.Length];
            Buffer.BlockCopy(path_len_array, 0, Payload, 0, 4);
            Buffer.BlockCopy(path_array, 0, Payload, 4, path_array.Length);
            Buffer.BlockCopy(data_len_array, 0, Payload, 4 + path_array.Length, 4);
            Buffer.BlockCopy(Data, 0, Payload, 8 + path_array.Length, Data.Length);
        }
    }
}
