using MessageBroker.Client.Network.Client;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Tests
{
    public class Tests
    {
        
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TcpKeepAliveTest()
        {
            var broker = new Broker("0.0.0.0", 6677);
            broker.Start();

            var client = BrokerClientFactory.GetClient("127.0.0.1", 6677);
            client.Connect();
            client.DeclareTopic("test", "/test/a");

            int recv_cnt = 0;
            var subscription = client.Subscribe("test");
            subscription.OnMessageReceived += (ea) =>
            {
                recv_cnt++;
                ea.Ack();
            };

            var event_slim = new ManualResetEvent(false);

            Task.Factory.StartNew(() =>
            {
                int i = 0;
                while (true)
                {
                    i++;
                    client.Publish("/test/a", new byte[] { 0x01, 0x02, 0x03, 0x04 });
                    Thread.Sleep(1000);

                    if (i == 10)
                    {
                        event_slim.Set();
                        break;
                    }
                }
            });

            broker.Stop();

            Thread.Sleep(5000);

            broker.Start();

            event_slim.WaitOne();

            Assert.IsTrue(recv_cnt >= 5);
        }
    }
}