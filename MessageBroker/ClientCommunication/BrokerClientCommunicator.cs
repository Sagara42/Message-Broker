using MessageBroker.Network.Message;
using MessageBroker.Network.Store.Base;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.ClientCommunication
{
    internal class BrokerClientCommunicator : IClientCommunication
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        private IClientStore _client_store;
        private ConcurrentQueue<InProcessMessage> _messages_to_sent;
        private ConcurrentDictionary<Guid, InProcessMessage> _in_process_messages;
        private const int _max_tries_count = 5;

        public BrokerClientCommunicator(IClientStore clientStore)
        {
            _client_store = clientStore;
            _messages_to_sent = new();
            _in_process_messages = new();

            //TODO: Remove message when client unsubscribed..
        }

        public void CheckMessages()
        {
            if(_messages_to_sent.TryDequeue(out var queued_message))
            {
                if (queued_message.TriesCount >= _max_tries_count)
                {
                    _log.Debug($"max tries reached for message {queued_message.Message.NetIdentity}, resent will stoped");
                    return;
                }

                SendMessageAsync(queued_message);
            }
            else
            {
                Thread.Sleep(20);
            }
        }

        private void SendMessageAsync(InProcessMessage message)
        {
            _= Task.Factory.StartNew((obj) =>
            {
                InProcessMessage message_to_sent = (InProcessMessage) obj;

                var client = _client_store.Get(message_to_sent.ClientId);
                if (client == null)
                    return;

                if (client.Socket.Connected == false)
                {
                    return; //todo: logs
                }

                client.SendMessage(message_to_sent.Message);

                if (_in_process_messages.TryAdd(message_to_sent.Message.NetIdentity, message_to_sent))
                {
                    message_to_sent.EventSlim.Wait(TimeSpan.FromSeconds(5));

                    if (_in_process_messages.TryRemove(message_to_sent.Message.NetIdentity, out var not_acked_message))
                    {
                        not_acked_message.TriesCount++;
                        not_acked_message.EventSlim.Reset();

                        _messages_to_sent.Enqueue(not_acked_message);

                        _log.Debug($"client {not_acked_message.ClientId} not acked for message {not_acked_message.Message.NetIdentity}, inqueue message to resent");
                    }
                }
            }, message);
        }

        public void Ack(Guid messageId)
        {
            if (_in_process_messages.TryRemove(messageId, out var in_process_message))
            {
                in_process_message.EventSlim.Set();

                _log.Debug($"Message {messageId} acked from client {in_process_message.ClientId}");
            }
        }

        public void Nack(Guid messageId)
        {
            if (_in_process_messages.TryGetValue(messageId, out var in_process_message))
            {
                in_process_message.EventSlim.Set();

                _log.Debug($"Message {messageId} nacked from client {in_process_message.ClientId}");
            }
        }

        public void HandleMessage(Guid clientId, BrokerMessage message)
        {
            _messages_to_sent.Enqueue(new InProcessMessage
            {
                ClientId = clientId,
                Message = message
            });
        }
    }

    internal class InProcessMessage
    {
        public Guid ClientId { get; set; }
        public BrokerMessage Message { get; set; }
        public ManualResetEventSlim EventSlim { get; set; } = new(false);
        public int TriesCount { get; set; }
    }
}
