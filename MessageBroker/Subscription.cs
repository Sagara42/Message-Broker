using System;

namespace MessageBroker
{
    public class Subscription
    {
        public Guid NetIdentity { get; private set; }
        public Guid ClientNetIdentity { get; private set; }

        public Subscription(Guid netIdentity, Guid clientNetIdentity)
        {
            NetIdentity = netIdentity;
            ClientNetIdentity = clientNetIdentity;
        }
    }
}