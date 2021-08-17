using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static IServiceCollection AddHttpClient<T>(this IServiceCollection services, Action<HttpClient> configureClient, IEnumerable<TimeSpan> sleepDurations = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureClient == null) throw new ArgumentNullException(nameof(configureClient));

            services
                .AddHttpClient(typeof(T).FullName, configureClient)
                .ConfigureHttpMessageHandlerBuilder(config => new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                })
                .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(sleepDurations ?? new[]
                {
                    TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)
                }));

            return services;
        }
    }
}
