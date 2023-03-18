namespace MessageBroker.Network.Store.Base
{
    public interface ITopicStore
    {
        void Add(string name, string path);
        void Remove(string name);
    }
}
