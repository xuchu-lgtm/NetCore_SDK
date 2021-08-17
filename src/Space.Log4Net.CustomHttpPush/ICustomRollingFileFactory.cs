using System.Collections.Concurrent;
using log4net.Appender;
using log4net.Core;

namespace Space.Log4Net
{
    public abstract class CustomRollingFileFactory : AppenderSkeleton
    {
        protected string ConnectionString { get; set; }
        protected virtual Level Level { get; set; } = Level.Warn;
        protected virtual int QueueLoggingCount { get; set; } = 10;
        protected readonly ConcurrentQueue<LoggingEvent> QueueLogging;
        protected CustomRollingFileFactory() => QueueLogging = new ConcurrentQueue<LoggingEvent>();
    }
}