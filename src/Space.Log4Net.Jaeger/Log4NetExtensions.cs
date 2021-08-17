using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Jaeger;
using Jaeger.Propagation;
using Jaeger.Samplers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Propagation;
using OpenTracing.Util;

namespace Space.Log4Net
{
    public static class Log4NetExtensions
    {
        public static IServiceCollection AddLog4Net(this IServiceCollection services, string serviceName)
        {
            services.AddOpenTracing();

            //https://www.alibabacloud.com/help/zh/doc-detail/99880.htm#title-wdi-s6z-hrq
            // 过滤httpclient采集中的Jaeger数据上报请求。
            //var httpOption = new HttpHandlerDiagnosticOptions();
            //httpOption.IgnorePatterns.Add(req => req.RequestUri.AbsolutePath.Contains("/api/traces"));
            //services.AddSingleton(Options.Create(httpOption));

            // Adds the Jaeger Tracer.
            services.AddSingleton<ITracer>(serviceProvider =>
            {

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                //var senderConfiguration = new Configuration.SenderConfiguration(loggerFactory)
                //    // 在链路追踪控制台获取Jaeger Endpoint。
                //    .WithEndpoint("http://tracing-analysis-dc-sz.aliyuncs.com/adapt_your_token/api/traces");


                // This will log to a default localhost installation of Jaeger.
                var tracer = new Tracer.Builder(serviceName)
                    .WithLoggerFactory(loggerFactory)
                    .WithSampler(new ConstSampler(true))
                    //.WithReporter(new RemoteReporter.Builder().WithFlushInterval(TimeSpan.FromSeconds(3)).WithSender(senderConfiguration.GetSender()).Build())
                    .RegisterCodec(BuiltinFormats.HttpHeaders, new TextMapCodec(true))
                    .RegisterCodec(BuiltinFormats.TextMap, new TextMapCodec(false))
                    .Build();

                // Allows code that can't use DI to also access the tracer.
                GlobalTracer.Register(tracer);

                return tracer;
            });

            return services;
        }

        internal static ILoggerFactory AddLog4Net([NotNull] this ILoggerFactory loggerFactory, string log4NetConfigFile, IGlobalTracerAccessor globalTracerAccessor, string environmentName)
        {
            if (!File.Exists(log4NetConfigFile))
                throw new ValidationException($"{nameof(log4NetConfigFile)} path not exists");

            loggerFactory.AddProvider(new Log4NetProvider(log4NetConfigFile, globalTracerAccessor, environmentName));
            return loggerFactory;
        }

        public static IApplicationBuilder UseLog4Net([NotNull] this IApplicationBuilder app, string environmentName) => UseLog4Net(app, "log4net.config", environmentName);

        public static IApplicationBuilder UseLog4Net([NotNull] this IApplicationBuilder app, [NotNull] string log4NetConfigFile, string environmentName)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (log4NetConfigFile == null) throw new ArgumentNullException(nameof(log4NetConfigFile));

            var globalTracerAccessor = app.ApplicationServices.GetRequiredService<IGlobalTracerAccessor>();

            app.ApplicationServices.GetRequiredService<ILoggerFactory>().AddLog4Net(log4NetConfigFile, globalTracerAccessor, environmentName);

            return app;
        }
    }
}
