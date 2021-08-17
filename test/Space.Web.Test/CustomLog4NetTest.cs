using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net.Core;
using Space.Log4Net;

namespace Space.Web.Test
{
    public class CustomLog4NetTest : CustomRollingFileAppender
    {
        protected override Level Level { get; set; } = Level.Warn;

        protected override int QueueLoggingCount { get; set; } = 2;

        protected override void PushLogs(LoggingEvent logging)
        {
            //throw new Exception("test");

         

            Console.WriteLine($"内部{logging.RenderedMessage}");
        }
    }
}
