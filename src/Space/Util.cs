using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Space
{
    public static class Util
    {
        public static void Polling(int retry, Action execute)
        {
            var index = 0;
            while (index++ < retry)
            {
                try
                {
                    execute();
                    return;
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public static string GetEndPoint()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = ((IPEndPoint)socket.LocalEndPoint).Address;

            return endPoint.ToString();
        }
    }
}
