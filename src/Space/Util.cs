using System;
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
    }
}
