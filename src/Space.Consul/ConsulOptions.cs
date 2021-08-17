using System.Collections.Generic;

namespace Space.Consul
{
    internal class ConsulOptions
    {
        public object Id { get; set; }
        public string Name { get; set; }
        public string[] Tags { get; set; }
        public string Address { get; set; }
        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    }
}
