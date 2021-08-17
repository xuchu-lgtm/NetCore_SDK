using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.Options;

namespace Space.DnsClient
{
    public interface IDnsClientFactory
    {
        Task<IPEndPoint> GetAddress();
    }

    internal class IPAddressProvider : IDnsClientFactory
    {
        private readonly IDnsQuery _dnsQuery;
        private readonly DnsEndpointOptions _options;

        public IPAddressProvider(IDnsQuery dnsQuery, IOptions<DnsEndpointOptions> options)
        {
            _dnsQuery = dnsQuery ?? throw new ArgumentNullException(nameof(dnsQuery));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<IPEndPoint> GetAddress()
        {
            var resolveService = await _dnsQuery.ResolveServiceAsync(_options.BaseDomain, _options.ServiceName.Replace(".", ""));

            return new IPEndPoint(resolveService?.First()?.AddressList?.FirstOrDefault() ?? throw new InvalidOperationException(nameof(resolveService)), resolveService?.First()?.Port ?? 0);
        }
    }
}
