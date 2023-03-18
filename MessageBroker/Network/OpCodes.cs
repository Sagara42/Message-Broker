namespace MessageBroker.Network
{
    public enum OpCodes : short
    {
        DeclareTopic = 0x01,
        DeleteTopic = 0x02,
        Publish = 0x03,
        Message = 0x04,
        Ack = 0x05,
        Nack = 0x06,
        Subscribe = 0x07,
        Unsubscribe = 0x08,
        Response = 0x09
    }
}
