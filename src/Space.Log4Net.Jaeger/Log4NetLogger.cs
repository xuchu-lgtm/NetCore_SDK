using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Xml;
using Jaeger;
using log4net;
using log4net.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Noop;
using OpenTracing.Tag;
using OpenTracing.Util;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Space.Log4Net
{
    internal class Log4NetLogger : ILogger
    {
        private const string OriginalFormatPropertyName = "{OriginalFormat}";
        private readonly IGlobalTracerAccessor _globalTracerAccessor;
        private readonly string _environmentName;
        private readonly ILog _log;

        public Log4NetLogger(IGlobalTracerAccessor globalTracerAccessor, string environmentName, string name, XmlElement xmlElement)
        {
            _globalTracerAccessor = globalTracerAccessor;
            _environmentName = environmentName;
            var loggerRepository = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            _log = LogManager.GetLogger(loggerRepository.Name, name);
            log4net.Config.XmlConfigurator.Configure(loggerRepository, xmlElement);
        }

        public IDisposable BeginScope<TState>(TState state) => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel)
        {
            // Filtering should be done via the general Logging filtering feature.
            var tracer = _globalTracerAccessor.GetGlobalTracer();
            return !(
                (tracer is NoopTracer) ||
                (tracer is GlobalTracer && !GlobalTracer.IsRegistered()));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                // This throws an Exception e.g. in Microsoft's DebugLogger but we don't want the app to crash if the logger has an issue.
                return;
            }

            var tracer = _globalTracerAccessor.GetGlobalTracer();
            var span = tracer.ActiveSpan;

            if (span == null)
            {
                // Creating a new span for a log message seems brutal so we ignore messages if we can't attach it to an active span.
                return;
            }

            if (!IsEnabled(logLevel))
            {
                return;
            }

            var logProperties = new LogProperties();
            var message = string.Empty;

            try
            {
                try
                {
                    // This throws if the argument count (message format vs. actual args) doesn't match.
                    // e.g. LogInformation("Foo {Arg1} {Arg2}", arg1);
                    // We want to preserve as much as possible from the original log message so we just continue without this information.
                    message = formatter(state, exception);
                }
                catch (Exception)
                {
                    /* no-op */
                }

                if (exception != null) WithException(logProperties, exception);


                var eventAdded = false;

                if (state is IEnumerable<KeyValuePair<string, object>> structure)
                {
                    try
                    {
                        // The enumerator throws if the argument count (message format vs. actual args) doesn't match.
                        // We want to preserve as much as possible from the original log message so we just ignore
                        // this error and take as many properties as possible.
                        foreach (var (key, value) in structure)
                        {
                            if (string.Equals(key, OriginalFormatPropertyName, StringComparison.Ordinal) && value is string messageTemplateString)
                            {
                                logProperties.Add(LogFields.Event, messageTemplateString);
                                eventAdded = true;
                            }
                            else
                            {
                                logProperties.Add(key, value);
                            }
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        /* no-op */
                    }
                }

                if (!eventAdded)
                {
                    logProperties.Add(LogFields.Event, "log");
                }

                WithLoggerName(logProperties, _log.Logger.Name);
                AttachedTracerProperties(span, tracer, logProperties);
                AttachedSystemProperties(logProperties);

                switch (logLevel)
                {
                    case LogLevel.Critical:
                        _log.Fatal(LogSerialize(message, Level.Critical, logProperties));
                        break;
                    case LogLevel.Debug:
                        _log.Debug(LogSerialize(message, Level.Debug, logProperties));
                        break;
                    case LogLevel.Trace:
                        _log.Debug(LogSerialize(message, Level.Trace, logProperties));
                        break;
                    case LogLevel.Error:
                        _log.Error(LogSerialize(message, Level.Error, logProperties));
                        break;
                    case LogLevel.Information:
                        _log.Info(LogSerialize(message, Level.Info, logProperties));
                        break;
                    case LogLevel.Warning:
                        _log.Warn(LogSerialize(message, Level.Warn, logProperties));
                        break;
                    case LogLevel.None:
                        break;
                    default:
                        _log.Info(LogSerialize(message, Level.Critical, logProperties));
                        break;
                }
            }
            catch (Exception ex)
            {
                WithException(logProperties, ex);
                _log.Error(LogSerialize(message, Level.Error, logProperties));
            }
        }

        private void AttachedTracerProperties(ISpan span, ITracer globalTracer, LogProperties properties)
        {
            properties.Add("LogCategory", "Default");
            properties.Add("Cluster", _environmentName);
            properties.Add("TraceId", span.Context.TraceId);
            properties.Add("SpanId", span.Context.SpanId);
            properties.Add("Version", "1.0.0");

            var scope = (Span)globalTracer.ScopeManager.Active.Span;
            properties.Add("ScopeId", scope.Context.SpanId.ToString());
            properties.Add("ParentScopeId", scope.Context.ParentId.ToString());
            properties.Add("ServerAddr", scope.Tracer.Tags[Constants.TracerIpTagKey]);
            properties.Add("AppName", scope.Tracer.ServiceName);

            var sessionId = scope.GetBaggageItem(span.Context.TraceId);
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N");

                if (scope.GetTags().TryGetValue(Tags.HttpUrl.Key, out var httpValue))
                {
                    properties.Add(Tags.HttpUrl.Key, httpValue);
                }

                if (scope.GetTags().TryGetValue(Tags.HttpMethod.Key, out var httpMethod))
                {
                    properties.Add(Tags.HttpMethod.Key, httpMethod);
                }

                scope.SetBaggageItem($"SeqId_{span.Context.TraceId}", "1");
                properties.Add("SeqId", 1);

                scope.SetBaggageItem(span.Context.TraceId, sessionId);
                properties.Add("SessionId", sessionId);
            }
            else
            {
                properties.Add("SessionId", sessionId);
                scope.SetBaggageItem(span.Context.TraceId, sessionId);

                if (int.TryParse(scope.GetBaggageItem($"SeqId_{span.Context.TraceId}"), out var seqIdResult))
                {
                    seqIdResult++;
                    properties.Add("SeqId", seqIdResult);
                    scope.SetBaggageItem($"SeqId_{span.Context.TraceId}", seqIdResult.ToString());
                }
                else
                {
                    scope.SetBaggageItem($"SeqId_{span.Context.TraceId}", "1");
                    properties.Add("SeqId", 1);
                }
            }

            var childSpan = globalTracer.BuildSpan(scope.Tracer.ServiceName).AsChildOf(globalTracer.ActiveSpan).Start();
            globalTracer.ScopeManager.Activate(childSpan, false);
            childSpan.Finish();
        }

        private static void WithLoggerName(LogProperties properties, string loggerName) => properties.Add("LoggerName", loggerName);

        private static void WithException(LogProperties properties, Exception exception) => properties.Add("Error", exception);

        private static void AttachedSystemProperties(LogProperties properties) => properties.Add("ThreadId", Thread.CurrentThread.ManagedThreadId);

        private static string LogSerialize(string message, Level level, LogProperties properties) => JsonConvert.SerializeObject(new
        {
            CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Level = level.ToString(),
            Message = message,
            Properties = properties.GetProperties(),
            CreatedOn = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
            Ver = "5"
        });

        private class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
