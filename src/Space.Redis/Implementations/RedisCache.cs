using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Space.Redis.Abstractions;
using Space.Redis.Configuration;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Space.Redis.Implementations
{
    internal sealed class RedisCache : IRedisCache, IDisposable
    {
        // KEYS[1] = = key
        // ARGV[1] = absolute-expiration - ticks as long (-1 for none)
        // ARGV[2] = sliding-expiration - ticks as long (-1 for none)
        // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
        // ARGV[4] = data - byte[]
        // this order should not change LUA script depends on it
        private const string SetScript = (@"
                redis.call('HMSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end
                return 1");

        private const string IncrScript = (@"
                redis.call('INCR', KEYS[1])
                if ARGV[2] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[2])
                end
                return 1");

        private const string AbsoluteExpirationKey = "absexp";
        private const string SlidingExpirationKey = "sldexp";
        private const string DataKey = "data";
        private const long NotPresent = -1;

        private volatile ConnectionMultiplexer _connection;
        private IDatabase _cache;

        private readonly RedisCacheOptions _options;
        private readonly string _instance;

        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        public RedisCache(IOptions<RedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            // This allows partitioning a single backend cache for use with multiple apps/services.
            _instance = _options.InstanceName ?? string.Empty;
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return GetAndRefresh(key, getData: true);
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            return await GetAndRefreshAsync(key, getData: true, token: token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Connect();

            var creationTime = DateTimeOffset.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            var result = _cache.ScriptEvaluate(SetScript, new RedisKey[] { _instance + key },
                new RedisValue[]
                {
                        absoluteExpiration?.Ticks ?? NotPresent,
                        options.SlidingExpiration?.Ticks ?? NotPresent,
                        GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                        value
                });
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            var creationTime = DateTimeOffset.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            await _cache.ScriptEvaluateAsync(SetScript, new RedisKey[] { _instance + key },
                new RedisValue[]
                {
                        absoluteExpiration?.Ticks ?? NotPresent,
                        options.SlidingExpiration?.Ticks ?? NotPresent,
                        GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                        value
                });
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            GetAndRefresh(key, getData: false);
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await GetAndRefreshAsync(key, getData: false, token: token);
        }

        private void Connect()
        {
            if (_cache != null)
            {
                return;
            }

            _connectionLock.Wait();
            try
            {
                if (_cache == null)
                {
                    if (_options.ConfigurationOptions != null)
                    {
                        _connection = ConnectionMultiplexer.Connect(_options.ConfigurationOptions);
                    }
                    else
                    {
                        _connection = ConnectionMultiplexer.Connect(_options.Configuration);
                    }
                    _cache = _connection.GetDatabase();

                    if (!string.IsNullOrEmpty(_options.KeyPrefix))
                    {
                        _cache = _cache.WithKeyPrefix(_options.KeyPrefix);
                    }
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task ConnectAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            if (_cache != null)
            {
                return;
            }

            await _connectionLock.WaitAsync(token);
            try
            {
                if (_cache == null)
                {
                    if (_options.ConfigurationOptions != null)
                    {
                        _connection = await ConnectionMultiplexer.ConnectAsync(_options.ConfigurationOptions);
                    }
                    else
                    {
                        _connection = await ConnectionMultiplexer.ConnectAsync(_options.Configuration);
                    }

                    _cache = _connection.GetDatabase();

                    if (!string.IsNullOrEmpty(_options.KeyPrefix))
                    {
                        _cache = _cache.WithKeyPrefix(_options.KeyPrefix);
                    }
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private byte[] GetAndRefresh(string key, bool getData)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Connect();

            // This also resets the LRU status as desired.
            // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
            RedisValue[] results;
            if (getData)
            {
                results = _cache.HashMemberGet(_instance + key, AbsoluteExpirationKey, SlidingExpirationKey, DataKey);
            }
            else
            {
                results = _cache.HashMemberGet(_instance + key, AbsoluteExpirationKey, SlidingExpirationKey);
            }

            // TODO: Error handling
            if (results.Length >= 2)
            {
                MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
                Refresh(key, absExpr, sldExpr);
            }

            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }

            return null;
        }

        private async Task<byte[]> GetAndRefreshAsync(string key, bool getData, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            // This also resets the LRU status as desired.
            // TODO: Can this be done in one operation on the server side? Probably, the trick would just be the DateTimeOffset math.
            RedisValue[] results;
            if (getData)
            {
                results = await _cache.HashMemberGetAsync(_instance + key, AbsoluteExpirationKey, SlidingExpirationKey, DataKey);
            }
            else
            {
                results = await _cache.HashMemberGetAsync(_instance + key, AbsoluteExpirationKey, SlidingExpirationKey);
            }

            // TODO: Error handling
            if (results.Length >= 2)
            {
                MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
                await RefreshAsync(key, absExpr, sldExpr, token);
            }

            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }

            return null;
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Connect();

            _cache.KeyDelete(_instance + key);
            // TODO: Error handling
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await ConnectAsync(token);

            await _cache.KeyDeleteAsync(_instance + key);
            // TODO: Error handling
        }

        public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return await _cache.KeyTimeToLiveAsync(_instance + key);
        }

        public async Task StringIncrementAsync(string key, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            var creationTime = DateTimeOffset.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            await _cache.ScriptEvaluateAsync(IncrScript, new RedisKey[] { _instance + key },
                new RedisValue[]
                {
                    1,
                    GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent
                });
        }

        public async Task<RedisValue> StringGetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return await _cache.StringGetAsync(_instance + key);
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flag = CommandFlags.None, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return await _cache.StringSetAsync(_instance + key, value, expiry, when, flag);
        }

        public async Task<bool> HashSetAsync(string hashKey, [NotNull] string key, string value, bool nx = false, CommandFlags flags = CommandFlags.None, CancellationToken token = default)
        {
            if (hashKey == null) throw new ArgumentNullException(nameof(hashKey));

            if (key == null) throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return await _cache.HashSetAsync(_instance + hashKey, key, value, nx ? When.NotExists : When.Always, flags);
        }

        public async Task HashSetAsync([NotNull] string hashKey, IDictionary<string, string> values, bool nx = false, CommandFlags flags = CommandFlags.None, CancellationToken token = default)
        {
            if (hashKey == null) throw new ArgumentNullException(nameof(hashKey));

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            await _cache.HashSetAsync(_instance + hashKey, values.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray(), flags);
        }

        public async Task<string> HashGetAsync(string hashKey, string key, CommandFlags flags = CommandFlags.None, CancellationToken token = default)
        {
            if (hashKey == null) throw new ArgumentNullException(nameof(hashKey));

            if (key == null) throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return await _cache.HashGetAsync(_instance + hashKey, key, flags);
        }

        public async Task<bool> UpdateExpiryAsync(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, CancellationToken token = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return (await Task.WhenAll(Task.Run(() => _cache.KeyExists(_instance + key, flags) && _cache.KeyExpire(_instance + key, expiry, flags), token)))[0];
        }

        public async Task<IReadOnlyDictionary<string, string>> HashGetAllAsync([NotNull] string hashKey, CommandFlags flags = CommandFlags.None, CancellationToken token = default)
        {
            if (hashKey == null) throw new ArgumentNullException(nameof(hashKey));

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return (await _cache.HashGetAllAsync(_instance + hashKey, flags)).ToDictionary(x => x.Name.ToString(), x => x.Value.ToString(), StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, string> HashGetAll([NotNull] string hashKey, CommandFlags flags = CommandFlags.None, CancellationToken token = default)
        {
            if (hashKey == null) throw new ArgumentNullException(nameof(hashKey));

            token.ThrowIfCancellationRequested();

            Connect();

            return (_cache.HashGetAll(_instance + hashKey, flags)).ToDictionary(x => x.Name.ToString(), x => x.Value.ToString(), StringComparer.Ordinal);
        }

        public async Task<bool> HashDeleteAsync([NotNull] string hashKey, [NotNull] string key, CommandFlags flags = CommandFlags.None, CancellationToken token = default)
        {
            if (hashKey == null) throw new ArgumentNullException(nameof(hashKey));
            if (key == null) throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            await ConnectAsync(token);

            return await _cache.HashDeleteAsync(_instance + hashKey, key, flags);
        }

        private void MapMetadata(RedisValue[] results, out DateTimeOffset? absoluteExpiration, out TimeSpan? slidingExpiration)
        {
            absoluteExpiration = null;
            slidingExpiration = null;
            var absoluteExpirationTicks = (long?)results[0];
            if (absoluteExpirationTicks.HasValue && absoluteExpirationTicks.Value != NotPresent)
            {
                absoluteExpiration = new DateTimeOffset(absoluteExpirationTicks.Value, TimeSpan.Zero);
            }
            var slidingExpirationTicks = (long?)results[1];
            if (slidingExpirationTicks.HasValue && slidingExpirationTicks.Value != NotPresent)
            {
                slidingExpiration = new TimeSpan(slidingExpirationTicks.Value);
            }
        }

        private void Refresh(string key, DateTimeOffset? absExpr, TimeSpan? sldExpr)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Note Refresh has no effect if there is just an absolute expiration (or neither).
            TimeSpan? expr = null;
            if (sldExpr.HasValue)
            {
                if (absExpr.HasValue)
                {
                    var relExpr = absExpr.Value - DateTimeOffset.Now;
                    expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
                }
                else
                {
                    expr = sldExpr;
                }
                _cache.KeyExpire(_instance + key, expr);
                // TODO: Error handling
            }
        }

        private async Task RefreshAsync(string key, DateTimeOffset? absExpr, TimeSpan? sldExpr, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            // Note Refresh has no effect if there is just an absolute expiration (or neither).
            TimeSpan? expr = null;
            if (sldExpr.HasValue)
            {
                if (absExpr.HasValue)
                {
                    var relExpr = absExpr.Value - DateTimeOffset.Now;
                    expr = relExpr <= sldExpr.Value ? relExpr : sldExpr;
                }
                else
                {
                    expr = sldExpr;
                }
                await _cache.KeyExpireAsync(_instance + key, expr);
                // TODO: Error handling
            }
        }

        private static long? GetExpirationInSeconds(DateTimeOffset creationTime, DateTimeOffset? absoluteExpiration, DistributedCacheEntryOptions options)
        {
            if (absoluteExpiration.HasValue && options.SlidingExpiration.HasValue)
            {
                return (long)Math.Min(
                    (absoluteExpiration.Value - creationTime).TotalSeconds,
                    options.SlidingExpiration.Value.TotalSeconds);
            }

            if (absoluteExpiration.HasValue)
            {
                return (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
            }

            if (options.SlidingExpiration.HasValue)
            {
                return (long)options.SlidingExpiration.Value.TotalSeconds;
            }
            return null;
        }

        private static DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset creationTime, DistributedCacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue && options.AbsoluteExpiration <= creationTime)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                    options.AbsoluteExpiration.Value,
                    "The absolute expiration value must be in the future.");
            }
            var absoluteExpiration = options.AbsoluteExpiration;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = creationTime + options.AbsoluteExpirationRelativeToNow;
            }

            return absoluteExpiration;
        }

        public void Dispose()
        {
            _connection?.Close();
        }
    }
}
