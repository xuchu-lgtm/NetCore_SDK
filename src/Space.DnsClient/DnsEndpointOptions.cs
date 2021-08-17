using System.Net;

namespace Space.DnsClient
{
    internal class DnsEndpointOptions
    {
        public string BaseDomain { get; set; } = "service.consul";
        /// <summary>
        /// 已注册的服务名称
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// Consul服务IP
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Consul服务端口
        /// </summary>
        public int Port { get; set; }
        public IPEndPoint GetIpEndPoint() => new(IPAddress.Parse(Address), Port);
    }
}
