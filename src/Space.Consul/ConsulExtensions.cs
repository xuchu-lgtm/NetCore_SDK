using System;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Space.Consul
{
    public static class ConsulExtensions
    {
        public static ISpaceBuilder AddConsul(this ISpaceBuilder builder)
        {
            var options = new ConsulOptions();
            builder.Configuration.Bind(nameof(ConsulOptions), options);

            builder.Services.Configure<ConsulOptions>(builder.Configuration.GetSection(nameof(ConsulOptions)));
            builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                consulConfig.Address = new Uri(options.Address);
            }));

            return builder;
        }

        public static IApplicationBuilder UseConsul(this IApplicationBuilder app)
        {
            var appLifetime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>() ?? throw new ArgumentException("Missing dependency", nameof(IApplicationLifetime));
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>() ?? throw new ArgumentException("Missing dependency", nameof(IConsulClient));
            var consulConfig = app.ApplicationServices.GetRequiredService<IOptions<ConsulOptions>>()?.Value ?? throw new ArgumentException("Missing dependency", nameof(IOptions<ConsulOptions>));

            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<IApplicationBuilder>();

            /* var features = app.Properties["server.Features"] as FeatureCollection;
             var addresses = features?.Get<IServerAddressesFeature>();
             var address = addresses?.Addresses.First();*/

            var address = Util.GetEndPoint();

            var uri = new Uri(address);
            var registration = new AgentServiceRegistration()
            {
                Check = new AgentServiceCheck()
                {
                    Status = HealthStatus.Passing,
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1), // 一分钟发现服务出错了，移除注册
                    Interval = TimeSpan.FromSeconds(30),
                    TCP = $"{uri.Host}:{uri.Port}"
                },
                ID = $"{consulConfig.Id ?? Guid.NewGuid()}-{uri.Port}",
                Name = consulConfig.Name ?? AppDomain.CurrentDomain.FriendlyName.Replace(".", ""),
                Address = uri.Host,
                Port = uri.Port,
                Tags = consulConfig.Tags,
                Meta = consulConfig.Meta
            };

            logger.LogInformation("Registering with Consul");
            consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            consulClient.Agent.ServiceRegister(registration).Wait();

            appLifetime.ApplicationStopping.Register(() =>
            {
                logger.LogInformation("Deregistering from Consul");
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });

            return app;
        }
    }
}
