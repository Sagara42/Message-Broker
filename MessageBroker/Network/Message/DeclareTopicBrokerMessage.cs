using System;
using System.Linq;
using System.Text;

namespace MessageBroker.Network.Message
{
    public class DeclareTopicBrokerMessage : IMessage
    {
        public Guid NetIdentity { get; private set; } = Guid.NewGuid();
        public short OpCode => (short) OpCodes.DeclareTopic.GetHashCode();
        public byte[] Payload { get; private set; }

        public string Name { get; private set; }
        public string Path { get; private set; }

        public DeclareTopicBrokerMessage(string name, string path)
        {
            Name = name;
            Path = path;

            SetupPayload();
        }

        public DeclareTopicBrokerMessage(Guid netIdentity, byte[] payload)
        {
            NetIdentity = netIdentity;
            Payload = payload;
        }

        public void RestoreFromPayload()
        {
            var name_len = BitConverter.ToInt16(Payload, 0);
            var name = Encoding.UTF8.GetString(Payload.Skip(2).Take(name_len).ToArray());
            var path_len = BitConverter.ToInt16(Payload, 2 + name_len);
            var path = Encoding.UTF8.GetString(Payload.Skip(4 + name_len).Take(path_len).ToArray());

            Name = name;
            Path = path;
        }

        private void SetupPayload()
        {
            var name_data = Encoding.UTF8.GetBytes(Name);
            var name_data_len = BitConverter.GetBytes((short) Name.Length);
            var path_data = Encoding.UTF8.GetBytes(Path);
            var path_data_len = BitConverter.GetBytes((short) Path.Length);

            Payload = new byte[name_data.Length + path_data.Length + 4];

            Buffer.BlockCopy(name_data_len, 0, Payload, 0, 2);
            Buffer.BlockCopy(name_data, 0, Payload, 2, name_data.Length);
            Buffer.BlockCopy(path_data_len, 0, Payload, 2 + name_data.Length, path_data_len.Length);
            Buffer.BlockCopy(path_data, 0, Payload, 4 + name_data.Length, path_data.Length);
        }
    }
}
