using MessageBroker.Client.Network.Message;
using System.Linq;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessageBroker.Client.Network.Serialization;
using MessageBroker.Client.Utils;
using System.Collections.Concurrent;
using System.Threading;

namespace MessageBroker.Client.Network.Client
{
    public class BrokerClient : IClient
    {
        public Socket Socket { get; private set; }
        private IPEndPoint _end_point;
        private IBrokerSerializer _serializer;
        private ConcurrentDictionary<Guid, Subscription> _subscriptions;
        private ConcurrentDictionary<Guid, InWaitResponeMessage> _in_wait_messages;
        private bool _is_disposed;

        public BrokerClient(string host, int port)
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _end_point = new IPEndPoint(IPAddress.Parse(host), port);
            _serializer = new BrokerSerializer();
            _subscriptions = new();
            _in_wait_messages = new();
        }

        public Subscription Subscribe(string topic_name)
        {
            var new_subscription_id = Guid.NewGuid();
            var subscribe_message = new SubscribeBrokerMessage(new_subscription_id, topic_name);
            var response = SendMessageAndWaitResponse(subscribe_message);
            if (response.ResponseType == ResponseType.Success)
            {
                var subscription = new Subscription(topic_name);
                _subscriptions.TryAdd(new_subscription_id, subscription);
                return subscription;
            }
            else
            {
                throw new Exception(response.ExceptionMessage);
            }
        }

        public void Unsubscribe(Guid subscription_id)
        {
            var unsubscribe_message = new UnsubscribeBrokerMessage(subscription_id);
            var response = SendMessageAndWaitResponse(unsubscribe_message);
            if(response.ResponseType == ResponseType.Success)
            {
                _subscriptions.TryRemove(subscription_id, out _);
            }
            else
            {
                throw new Exception(response.ExceptionMessage);
            }
        }

        public void Publish(string path, byte[] data)
        {
            var message = new PublishBrokerMessage(path, data);
            _= SendMessageAndWaitResponse(message);
        }

        public void DeclareTopic(string name, string path)
        {
            var message = new DeclareTopicBrokerMessage(name, path);
            var response = SendMessageAndWaitResponse(message);
            if (response.ResponseType == ResponseType.Exception)
                throw new Exception(response.ExceptionMessage);
        }

        public void DeleteTopic(string name)
        {
            var message = new DeleteTopicBrokerMessage(name);
            var response = SendMessageAndWaitResponse(message);
            if (response.ResponseType == ResponseType.Exception)
                throw new Exception(response.ExceptionMessage);
        }

        private InWaitResponeMessage SendMessageAndWaitResponse(IMessage message)
        {
            var in_wait_message = new InWaitResponeMessage { MessageId = message.NetIdentity };

            _ = _in_wait_messages.TryAdd(message.NetIdentity, in_wait_message);

            Send(message);

            in_wait_message.EventSlim.Wait(TimeSpan.FromSeconds(5));

            if (_in_wait_messages.TryRemove(message.NetIdentity, out var response))
            {
                return response;
            }

            throw new Exception();
        }

        public void Send(IMessage message)
        {
            if(Socket != null && Socket.Connected)
            {
                Socket.Send(_serializer.Serialize(message));
            }
        }

        public void Connect()
        {
            _is_disposed = false;
            try
            {
                Socket.Connect(_end_point);
            }
            catch
            {
                Reconnect();
            }

            StartReceiveMessages();
        }

        public void Disconnect()
        {
            _is_disposed = true;

            Socket?.Disconnect(reuseSocket: true);

            _subscriptions.Clear();
            
            foreach (var in_wait in _in_wait_messages)
                in_wait.Value.EventSlim.Set();

            _in_wait_messages.Clear();
        }

        private void Reconnect()
        {
            if (_is_disposed == true)
                return;

            Connect();

            foreach(var sub in _subscriptions.Values)            
                Send(new SubscribeBrokerMessage(sub.Id, sub.TopicName));           
        }

        private void StartReceiveMessages()
        {
            Task.Factory.StartNew(() =>
            {
                while (Socket.Connected)
                {
                    try
                    {
                        var message_len_array = Socket.ReadFromNetworkStream(4);
                        var message_len = BitConverter.ToInt32(message_len_array);
                        var message_array = Socket.ReadFromNetworkStream(message_len);
                        var opcode = BitConverter.ToInt16(message_array);
                        var message = GetMessage(opcode, message_len_array, message_array);

                        Task.Factory.StartNew(() => HandleMessage(message));
                    }
                    catch
                    {
                        Socket?.Disconnect(true);
                        Reconnect();
                    }
                }
            });
        }

        private IMessage GetMessage(short opcode, byte[] len, byte[] message)
        {
            var full_message = len.Concat(message).ToArray();
            if ((OpCodes)opcode == OpCodes.Response)
                return _serializer.Deserialize<ResponseBrokerMessage>(full_message);
            if ((OpCodes)opcode == OpCodes.Message)
                return _serializer.Deserialize<BrokerMessage>(full_message);

            throw new Exception($"{opcode} not supported message type");
        }

        private void HandleMessage(IMessage message)
        {
            if (message is BrokerMessage)
                PublishReceived(message as BrokerMessage);

            if (message is ResponseBrokerMessage)
                ResponseReceived(message as ResponseBrokerMessage);
        }

        private void PublishReceived(BrokerMessage message) 
        {
            var subscription = _subscriptions
                .ContainsKey(message.SubscribeNetIdentity) ? _subscriptions[message.SubscribeNetIdentity] : null;

            try
            {
                subscription?.OnMessageReceived?.Invoke(new PublishData(this, message.NetIdentity, message.Data));
            }
            catch
            {
                Send(new NackBrokerMessage(message.NetIdentity));
            }
        }

        private void ResponseReceived(ResponseBrokerMessage message)
        {
            var in_wait_message = _in_wait_messages
                .ContainsKey(message.OnMessageResponseId) ? _in_wait_messages[message.OnMessageResponseId] : null;

            if(in_wait_message != null)
            {
                in_wait_message.ResponseType = message.ResponseType;
                in_wait_message.ExceptionMessage = message.ExceptionMessage;
                in_wait_message.EventSlim.Set();
            }
        }
    }

    internal class InWaitResponeMessage
    {
        public Guid MessageId { get; set; }
        public ManualResetEventSlim EventSlim { get; set; } = new();
        public ResponseType ResponseType { get; set; }
        public string ExceptionMessage { get; set; }
    }
}
