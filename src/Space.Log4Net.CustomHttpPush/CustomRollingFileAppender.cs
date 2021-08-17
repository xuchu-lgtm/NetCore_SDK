using System.Linq;
using System.Threading.Tasks;
using log4net.Core;

namespace Space.Log4Net
{
    public class CustomRollingFileAppender : CustomRollingFileFactory
    {
        protected override void Append(LoggingEvent loggingEvent) => BuildDocument(loggingEvent);

        protected LoggingEvent BuildDocument(LoggingEvent loggingEvent)
        {
            if (loggingEvent.Level < Level) return loggingEvent;

            Task.Run(() =>
            {
                QueueLogging.Enqueue(loggingEvent);

                EventDequeue();
            });

            return loggingEvent;
        }

        protected override void Append(LoggingEvent[] loggingEvent) => _ = loggingEvent.Select(BuildDocument);

        private void EventDequeue()
        {
            if (QueueLogging.Count < QueueLoggingCount) return;

            while (QueueLogging.TryDequeue(out var __)) Util.Polling(3, () => PushLogs(__));
        }

        protected virtual void PushLogs(LoggingEvent logging) { }
    }
}
