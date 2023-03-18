using MessageBroker.Network.Message;

namespace MessageBroker.Network.Serialization
{
    public interface IBrokerSerializer
    {
        byte[] Serialize(IMessage message);
        T Deserialize<T>(byte[] data) where T : IMessage;
    }
}
