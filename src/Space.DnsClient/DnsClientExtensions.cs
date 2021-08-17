using System;
using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Space.DnsClient
{
    public static class DnsClientExtensions
    {
        public static ISpaceBuilder AddDnsClient(this ISpaceBuilder builder)
        {
            builder.Services.Configure<DnsEndpointOptions>(builder.Configuration.GetSection(nameof(DnsEndpointOptions)));

            builder.Services.AddSingleton<IDnsQuery>(x => new LookupClient(new LookupClientOptions(x.GetRequiredService<IOptions<DnsEndpointOptions>>().Value.GetIpEndPoint())
            {
                EnableAuditTrail = false,
                UseCache = true,
                MinimumCacheTimeout = TimeSpan.FromSeconds(1)
            }));
            builder.Services.AddSingleton<IDnsClientFactory, IPAddressProvider>();

            return builder;
        }
    }
}
