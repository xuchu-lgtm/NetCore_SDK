using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Space.Redis.Abstractions;
using Space.Redis.Configuration;
using Space.Redis.Implementations;

namespace Space.Cache
{
    /// <summary>
    /// Extension methods for setting up Redis distributed cache related services in an <see cref="ISpaceBuilder" />.
    /// </summary>
    public static class StackExchangeRedisCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Redis distributed caching services to the specified <see cref="ISpaceBuilder" />.
        /// </summary>
        /// <param name="services">The <see cref="ISpaceBuilder" /> to add services to.</param>
        /// <returns>The <see cref="ISpaceBuilder"/> so that additional calls can be chained.</returns>
        public static ISpaceBuilder AddRedisCache([NotNull] this ISpaceBuilder services)
        {
            return AddRedisCache(services, x =>
            {
                x.InstanceName = "rds";
                x.Configuration = "127.0.0.1";
            });
        }

        /// <summary>
        /// Adds Redis distributed caching services to the specified <see cref="ISpaceBuilder" />.
        /// </summary>
        /// <param name="services">The <see cref="ISpaceBuilder" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{RedisCacheOptions}"/> to configure the provided
        /// <see cref="RedisCacheOptions"/>.</param>
        /// <returns>The <see cref="ISpaceBuilder"/> so that additional calls can be chained.</returns>
        public static ISpaceBuilder AddRedisCache([NotNull] this ISpaceBuilder services, Action<RedisCacheOptions> setupAction)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

            services.Services.Configure(setupAction);
            services.Services.Add(ServiceDescriptor.Singleton<IRedisCache, RedisCache>());

            return services;
        }
    }
}
