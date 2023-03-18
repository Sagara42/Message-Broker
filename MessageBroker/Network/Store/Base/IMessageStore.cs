namespace MessageBroker.Network.Store.Base
{
    public interface IMessageStore
    {
        void Publish(string path, byte[] data);
    }
}
