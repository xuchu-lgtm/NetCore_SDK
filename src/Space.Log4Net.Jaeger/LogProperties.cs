using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Space.Log4Net
{
    internal class LogProperties
    {
        private readonly ConcurrentDictionary<string, object> _items;

        public LogProperties() => _items = new ConcurrentDictionary<string, object>();

        public void Add(string key, int value) => _items[key] = value;

        public void Add(string key, string value) => _items[key] = value;

        public void Add(string key, object value) => _items[key] = value;

        public IReadOnlyDictionary<string, object> GetProperties() => _items;
    }
}
