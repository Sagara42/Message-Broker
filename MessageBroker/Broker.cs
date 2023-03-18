using MessageBroker.Network;
using MessageBroker.Network.Client;
using MessageBroker.Network.Message;
using MessageBroker.Network.Store;
using MessageBroker.Network.Store.Base;
using MessageBroker.Utils;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MessageBroker
{
    public class Broker
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly TcpServer _server;

        private readonly ConcurrentDictionary<Guid, Topic> _topics;

        private readonly IClientStore _client_store;
        private ITopicStore _topic_store;
        private IMessageStore _message_store;

        public Broker(string host, int port)
        {
            _server = new TcpServer(host, port);
            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;
            _server.OnMessageReceived += OnMessageReceived;
            _topics = new();
            _client_store = new ClientStore();
        }

        public void Start()
        {
            _server.Start();
        }

        public void Stop()
        {
            foreach (var topic in _topics.Values)
                topic.StopMessageHandling();

            _topics.Clear();
            _server.Stop();
        }

        public void UseTopicStore(ITopicStore store) => _topic_store = store;
        
        public void UseMessageStore(IMessageStore store) => _message_store = store;

        private void OnClientConnected(IClient client) 
        { 
            _client_store.Add(client);

            _log.Debug($"client {client.NetIdentity} connected.");
        }
        
        private void OnClientDisconnected(IClient client) 
        {
            _log.Debug($"client {client.NetIdentity} disconnected.");

            _client_store.Remove(client.NetIdentity);
            foreach (var topic in _topics.Values)
                topic.OnClientDisconnected(client.NetIdentity);
        }

        #region Message handling
        
        private void OnMessageReceived(IClient client, IMessage message) 
        {
            if (message is DeclareTopicBrokerMessage)
                OnDeclareTopicReceived(client, message as DeclareTopicBrokerMessage);

            if(message is DeleteTopicBrokerMessage)
                OnDeleteTopicReceived(client, message as DeleteTopicBrokerMessage);

            if(message is SubscribeBrokerMessage)
                OnSubscribeReceived(client, message as SubscribeBrokerMessage);

            if(message is UnsubscribeBrokerMessage)
                OnUnsubscribeReceived(client, message as UnsubscribeBrokerMessage);

            if(message is PublishBrokerMessage)
                OnPublishReceived(client, message as PublishBrokerMessage);

            if(message is AckBrokerMessage)
                OnAckReceived(client, message as AckBrokerMessage);

            if(message is NackBrokerMessage)
                OnNackReceived(client, message as NackBrokerMessage);
        }

        private void OnDeclareTopicReceived(IClient client, DeclareTopicBrokerMessage message)
        {
            foreach (var topic in _topics.Values)
            {
                if (topic.Name == message.Name)
                {
                    client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Exception, $"Topic {message.Name} already declared"));
                    return;
                }
            }

            var new_topic = new Topic(message.Name, message.Path, _client_store);
            new_topic.StartMessageHandling();

            _ = _topics.TryAdd(new_topic.Identity, new_topic);

            _topic_store?.Add(new_topic.Name, new_topic.Path);

            client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Success));

            _log.Debug($"topic {new_topic.Name} : {new_topic.Path} declared.");
        }

        private void OnDeleteTopicReceived(IClient client, DeleteTopicBrokerMessage message)
        {
            var topic = _topics.Values.FirstOrDefault(s => s.Name == message.Name);
            if(topic != null)
            {
                topic.StopMessageHandling();
                
                _ = _topics.Remove(topic.Identity, out _);
                
                _topic_store?.Remove(topic.Name);
                
                client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Success));

                _log.Debug($"topic {topic.Name} : {topic.Path} deleted");
            }
            else
            {
                client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Exception, "Topic not declared"));
            }
        }
    
        private void OnSubscribeReceived(IClient client, SubscribeBrokerMessage message)
        {
            var topic = _topics.Values.FirstOrDefault(s => s.Name == message.TopicName);
            if(topic == null)
            {
                client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Exception, $"Topic {message.TopicName} not declared."));
                return;
            }
            
            topic.AddSubscription(client.NetIdentity, message.SubscribeNetIdentity);
            client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Success));

            _log.Debug($"client {client.NetIdentity} subscribed to {topic.Path}");
        }

        private void OnUnsubscribeReceived(IClient client, UnsubscribeBrokerMessage message)
        {
            foreach (var topic in _topics.Values)
                topic.RemoveSubscription(message.SubscribeNetIdentity);

            client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Success));
        }
    
        private void OnPublishReceived(IClient client, PublishBrokerMessage message) 
        {
            foreach (var topic in _topics.Values)
            {
                if (topic.IsPathMatch(message.Path))
                {
                    topic.Publish(message.Data);                   
                }
            }

            _message_store?.Publish(message.Path, message.Data);

            client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Success));

            _log.Debug($"client {client.NetIdentity} published to {message.Path}\n{message.Data.FormatHex()}");
        }
    
        private void OnAckReceived(IClient client, AckBrokerMessage message)
        {
            foreach (var topic in _topics.Values)
                topic.Ack(message.MessageNetIdentity);

            client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Success));
        }

        private void OnNackReceived(IClient client, NackBrokerMessage message)
        {
            foreach (var topic in _topics.Values)
                topic.Nack(message.MessageNetIdentity);

            client.SendMessage(new ResponseBrokerMessage(message.NetIdentity, ResponseType.Success));
        }
        
        #endregion
    }
}