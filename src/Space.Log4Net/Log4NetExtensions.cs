using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Space.Log4Net
{
    public static class Log4NetExtensions
    {
        internal static ILoggerFactory AddLog4Net([NotNull] this ILoggerFactory loggerFactory, string log4NetConfigFile)
        {
            if (!File.Exists(log4NetConfigFile))
                throw new ValidationException($"{nameof(log4NetConfigFile)} path not exists");

            loggerFactory.AddProvider(new Log4NetProvider(log4NetConfigFile));
            return loggerFactory;
        }

        public static IApplicationBuilder UseLog4Net([NotNull] this IApplicationBuilder app) => UseLog4Net(app, "log4net.config");

        public static IApplicationBuilder UseLog4Net([NotNull] this IApplicationBuilder app, [NotNull] string log4NetConfigFile)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (log4NetConfigFile == null) throw new ArgumentNullException(nameof(log4NetConfigFile));


            app.ApplicationServices.GetRequiredService<ILoggerFactory>().AddLog4Net(log4NetConfigFile);

            return app;
        }
    }
}
