using MessageBroker.Network.Client;
using MessageBroker.Network.Message;
using MessageBroker.Network.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Network
{
    internal class TcpServer
    {
        private readonly HashSet<Thread> _listeningThreads = new();
        private Socket _listeningSocket;

        private readonly string _host;
        private readonly int _port;

        private readonly IBrokerSerializer _serializer;

        private const int AcceptThreadsNum = 15;

        public Action<IClient> OnClientConnected;
        public Action<IClient> OnClientDisconnected;
        public Action<IClient, IMessage> OnMessageReceived;

        public TcpServer(string host, int port)
        {
            _host = host;
            _port = port;
            _serializer = new BrokerSerializer();
        }

        public void Start()
        {
            _listeningSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listeningSocket.Bind(new IPEndPoint(IPAddress.Parse(_host), _port));
            _listeningSocket.Listen(50);

            for (int i = 0; i < AcceptThreadsNum; i++)
            {
                var th = new Thread(() => _listeningSocket.BeginAccept(AcceptCallback, null));
                th.Start();
                _listeningThreads.Add(th);
            }
        }

        public void Stop()
        {
            _listeningSocket.Close();
            _listeningThreads.Clear();
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            Socket socket = null;
            try
            {
                socket = _listeningSocket.EndAccept(asyncResult);
                Task.Factory.StartNew(() => StartHandlingClientMessages(socket), TaskCreationOptions.LongRunning);
            }
            catch
            {
                socket?.Disconnect(false);
                socket?.Dispose();
            }

            try
            {
                _listeningSocket.BeginAccept(AcceptCallback, null);
            }
            catch
            {

            }
        }

        private void StartHandlingClientMessages(Socket socket)
        {
            var client = new BrokerClient(socket, _serializer);
            
            OnClientConnected?.Invoke(client);

            while (socket.Connected)
            {
                try
                {
                    var message_len_array = ReadFromNetworkStream(socket, 4);
                    var message_len = BitConverter.ToInt32(message_len_array);
                    var message_array = ReadFromNetworkStream(socket, message_len);
                    var opcode = BitConverter.ToInt16(message_array);
                    var message = GetMessage(opcode, message_len_array, message_array);

                    OnMessageReceived?.Invoke(client, message);
                }
                catch (Exception ex)
                {
                    socket?.Dispose();
                    OnClientDisconnected?.Invoke(client);
                }
            }
        }

        private IMessage GetMessage(short opcode, byte[] len, byte[] message)
        {
            var full_message = len.Concat(message).ToArray();
            if((OpCodes) opcode == OpCodes.DeclareTopic)
                return _serializer.Deserialize<DeclareTopicBrokerMessage>(full_message);
            if ((OpCodes)opcode == OpCodes.DeleteTopic)
                return _serializer.Deserialize<DeleteTopicBrokerMessage>(full_message);
            if ((OpCodes)opcode == OpCodes.Publish)
                return _serializer.Deserialize<PublishBrokerMessage>(full_message);
            if((OpCodes) opcode == OpCodes.Message)
                return _serializer.Deserialize<BrokerMessage>(full_message);
            if ((OpCodes)opcode == OpCodes.Ack)
                return _serializer.Deserialize<AckBrokerMessage>(full_message);
            if ((OpCodes)opcode == OpCodes.Nack)
                return _serializer.Deserialize<NackBrokerMessage>(full_message);
            if ((OpCodes)opcode == OpCodes.Subscribe)
                return _serializer.Deserialize<SubscribeBrokerMessage>(full_message);
            if ((OpCodes)opcode == OpCodes.Unsubscribe)
                return _serializer.Deserialize<UnsubscribeBrokerMessage>(full_message);

            throw new Exception($"{opcode} not supported message type");
        }

        private byte[] ReadFromNetworkStream(Socket socket, int length)
        {
            var counter = 0;
            var buffer = new byte[length];

            while (counter != length)
            {
                var readed = socket.Receive(buffer, counter, length - counter, SocketFlags.None);
                if (readed == 0)
                    break;

                counter += readed;
            }

            return buffer;
        }
    }
}
