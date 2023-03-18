using System.Net.Sockets;

namespace MessageBroker.Client.Utils
{
    public static class NetworkUtils
    {
        public static byte[] ReadFromNetworkStream(this Socket socket, int length)
        {
            var counter = 0;
            var buffer = new byte[length];

            while (counter != length)
            {
                var readed = socket.Receive(buffer, counter, length - counter, SocketFlags.None);
                if (readed == 0)
                    break;

                counter += readed;
            }

            return buffer;
        }
    }
}
