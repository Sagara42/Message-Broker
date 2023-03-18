using MessageBroker.ClientCommunication;
using MessageBroker.Network.Message;
using MessageBroker.Network.Store.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker
{
    internal class Topic
    {
        public Guid Identity { get; } = Guid.NewGuid();
        public string Name { get; private set; }
        public string Path { get; private set; }

        private CancellationTokenSource _cancellation_token;
        private ConcurrentDictionary<Guid, Subscription> _subscriptions;

        private IClientCommunication _client_communication;

        public Topic(string name, string path, IClientStore clientStore)
        {
            Name = name;
            Path = path;

            _cancellation_token = new();
            _subscriptions = new();

            _client_communication = new BrokerClientCommunicator(clientStore);
        }

        public bool IsPathMatch(string messagePath)
        {
            if (messagePath is null) return false;

            const string wildCard = "*";

            var messageRouteSegments = messagePath.Split('/');
            var queueRouteSegments = Path.Split('/');

            var minSegmentCount = Math.Min(messageRouteSegments.Length, queueRouteSegments.Length);

            for (var i = 0; i < minSegmentCount; i++)
            {
                var messageSegment = messageRouteSegments[i];
                var queueSegment = queueRouteSegments[i];

                if (messageSegment == wildCard || queueSegment == wildCard)
                    continue;

                if (messageSegment == queueSegment)
                    continue;

                return false;
            }

            return true;
        }
    
        public void StartMessageHandling()
        {
            Task.Factory.StartNew(() =>
            {
                while (!_cancellation_token.IsCancellationRequested)
                {
                    _client_communication.CheckMessages();
                }
            });
        }

        public void StopMessageHandling()
        {
            _cancellation_token.Cancel();
        }
    
        public void AddSubscription(Guid clientNetId, Guid subscriptionId)
        {
            if (_subscriptions.ContainsKey(clientNetId))
                throw new Exception($"Cant add new subscription for client {clientNetId} already subscribed..");

            _= _subscriptions.TryAdd(clientNetId, new Subscription(subscriptionId, clientNetId));
        }

        public void RemoveSubscription(Guid subscriptionId)
        {
            var subscription = _subscriptions.Values.FirstOrDefault(s => s.NetIdentity == subscriptionId);
            if (subscription == null)
                return;

            _= _subscriptions.Remove(subscription.ClientNetIdentity, out _);
        }
        
        public void OnClientDisconnected(Guid clientId)
        {
            if(_subscriptions.ContainsKey(clientId))
            {
                _subscriptions.Remove(clientId, out _);
            }
        }

        public void Publish(byte[] data)
        {
            foreach (var subscription in _subscriptions)
                _client_communication.HandleMessage(subscription.Value.ClientNetIdentity, new BrokerMessage(subscription.Value.NetIdentity, data));
        }

        public void Ack(Guid messageId) { _client_communication.Ack(messageId); }
        
        public void Nack(Guid messageId) { _client_communication.Nack(messageId); }
    }
}