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
        private ManualResetEventSlim _manual_reset_event;

        public BrokerClient(string host, int port)
        {           
            _end_point = new IPEndPoint(IPAddress.Parse(host), port);
            _serializer = new BrokerSerializer();
            _subscriptions = new();
            _in_wait_messages = new();
            _manual_reset_event = new(false);
        }

        public Subscription Subscribe(string topic_name)
        {
            var new_subscription_id = Guid.NewGuid();
            var subscribe_message = new SubscribeBrokerMessage(new_subscription_id, topic_name);
            var response = SendMessageAndWaitResponse(subscribe_message);
            if (response.ResponseType == ResponseType.Success)
            {
                var subscription = new Subscription(new_subscription_id, topic_name);
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

        public bool Send(IMessage message)
        {
            try
            {
                if (Socket != null && Socket.Connected)
                {
                    var data = _serializer.Serialize(message);

                    return Socket.Send(data) == data.Length;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Send ex {ex.Message}");
            }

            return false;
        }

        public void Connect()
        {
            _manual_reset_event.Reset();
            
            _is_disposed = false;

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connect_task = Socket.ConnectAsync(_end_point);

            Task.WaitAny(new[] { connect_task }, (int)TimeSpan.FromSeconds(5).TotalMilliseconds);

            if (Socket.Connected)
            {
                StartReceiveMessages();
            }
            else
            {
                Reconnect();
            }
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
            Console.WriteLine("Reconnect");

            if (Socket != null && Socket.Connected)
                return;

            if (_is_disposed == true)
                return;

            _manual_reset_event.Set();

            Thread.Sleep(2000);

            Connect();

            if (Socket.Connected)
                foreach (var sub in _subscriptions.Values)
                    SendMessageAndWaitResponse(new SubscribeBrokerMessage(sub.Id, sub.TopicName));    
        }

        private void StartReceiveMessages()
        {
            Task.Factory.StartNew(() =>
            {
                while (Socket.Connected && _manual_reset_event.IsSet == false)
                {
                    try
                    {
                        var message_len_array = Socket.ReadFromNetworkStream(4);
                        var message_len = BitConverter.ToInt32(message_len_array);
                        if (message_len == 0)
                            throw new Exception("connection lost");
                        
                        if (message_len < 0 || message_len >= short.MaxValue)
                            throw new Exception("stream corrupted");

                        var message_array = Socket.ReadFromNetworkStream(message_len);

                        var opcode = BitConverter.ToInt16(message_array);
                        var message = GetMessage(opcode, message_len_array, message_array);

                        Task.Factory.StartNew(() => HandleMessage(message));
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Socket?.Shutdown(SocketShutdown.Both);
                        Socket?.Disconnect(false);
                        Socket?.Close();
                        Socket?.Dispose();
                        Reconnect();
                        break;
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
