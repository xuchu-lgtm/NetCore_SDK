﻿using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Space.Log4Net
{
    internal class Log4NetProvider : ILoggerProvider
    {
        private readonly string _log4NetConfigFile;

        private readonly ConcurrentDictionary<string, Log4NetLogger> _loggers = new();

        public Log4NetProvider(string log4NetConfigFile) => _log4NetConfigFile = log4NetConfigFile;

        public void Dispose() => _loggers.Clear();

        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);

        private Log4NetLogger CreateLoggerImplementation(string name) => new(name, ParseLog4NetConfigFile(_log4NetConfigFile));

        private static XmlElement ParseLog4NetConfigFile(string fileName)
        {
            var log4NetConfig = new XmlDocument();
            log4NetConfig.Load(File.OpenRead(fileName));
            return log4NetConfig["log4net"];
        }
    }
}
