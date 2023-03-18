using MessageBroker.Client.Network.Message;

namespace MessageBroker.Client.Network.Serialization
{
    public interface IBrokerSerializer
    {
        byte[] Serialize(IMessage message);
        T Deserialize<T>(byte[] data) where T : IMessage;
    }
}
