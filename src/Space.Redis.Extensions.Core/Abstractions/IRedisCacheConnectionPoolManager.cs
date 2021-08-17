using System;
using StackExchange.Redis;

namespace Space.Redis.Extensions.Core.Abstractions
{
    public interface IRedisCacheConnectionPoolManager : IDisposable
    {
        IConnectionMultiplexer GetConnection();
    }
}
