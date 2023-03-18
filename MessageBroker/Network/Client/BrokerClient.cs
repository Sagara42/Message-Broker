using MessageBroker.Network.Message;
using MessageBroker.Network.Serialization;
using System;
using System.Net.Sockets;

namespace MessageBroker.Network.Client
{
    internal class BrokerClient : IClient
    {
        public Guid NetIdentity { get; private set; }
        public Socket Socket { get; private set; }
        
        private IBrokerSerializer _serializer;

        public BrokerClient(Socket socket, IBrokerSerializer serializer)
        {
            NetIdentity = Guid.NewGuid();

            Socket = socket;
            _serializer = serializer;
        }

        public void SendMessage(IMessage message) 
        {
            if (Socket.Connected)
            {
                var data = _serializer.Serialize(message);

                Socket.Send(data);
            }
        }
    }
}
