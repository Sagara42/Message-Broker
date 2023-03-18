namespace MessageBroker.Client.Network.Client
{
    public static class BrokerClientFactory
    {
        public static IClient GetClient(string host, int port)
        {
            return new BrokerClient(host, port);
        }
    }
}
