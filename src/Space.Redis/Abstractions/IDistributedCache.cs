using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Space.Redis.Configuration;
using StackExchange.Redis;

namespace Space.Redis.Abstractions
{
    /// <summary>
    /// Represents a distributed cache of serialized values.
    /// </summary>
    public interface IRedisCache
    {
        /// <summary>
        /// Gets a value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <returns>The located value or null.</returns>
        byte[] Get(string key);

        /// <summary>
        /// Gets a value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the located value or null.</returns>
        Task<byte[]> GetAsync(string key, CancellationToken token = default);

        /// <summary>
        /// Sets a value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="value">The value to set in the cache.</param>
        /// <param name="options">The cache options for the value.</param>
        void Set(string key, byte[] value, DistributedCacheEntryOptions options);

        /// <summary>
        /// Sets the value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="value">The value to set in the cache.</param>
        /// <param name="options">The cache options for the value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default);

        /// <summary>
        /// Refreshes a value in the cache based on its key, resetting its sliding expiration timeout (if any).
        /// </summary>
        /// <param name="key">A string identifying the requested calue.</param>
        void Refresh(string key);

        /// <summary>
        /// Refreshes a value in the cache based on its key, resetting its sliding expiration timeout (if any).
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task RefreshAsync(string key, CancellationToken token = default);

        /// <summary>
        /// Removes the value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        void Remove(string key);

        /// <summary>
        /// Removes the value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task RemoveAsync(string key, CancellationToken token = default);

        Task<TimeSpan?> GetTtlAsync(string key, CancellationToken token = default);

        Task StringIncrementAsync(string key, DistributedCacheEntryOptions options, CancellationToken token = default);

        Task<RedisValue> StringGetAsync(string key, CancellationToken token = default);

        Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, CancellationToken token = default);

        Task<bool> HashSetAsync(string hashKey, string key, string value, bool nx = false, CommandFlags flags = CommandFlags.None, CancellationToken token = default);

        Task HashSetAsync(string hashKey, IDictionary<string, string> values, bool nx = false, CommandFlags flags = CommandFlags.None, CancellationToken token = default);

        Task<string> HashGetAsync(string hashKey, string key, CommandFlags flags = CommandFlags.None, CancellationToken token = default);

        Task<bool> UpdateExpiryAsync(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, CancellationToken token = default);

        Task<IReadOnlyDictionary<string, string>> HashGetAllAsync(string hashKey, CommandFlags flags = CommandFlags.None, CancellationToken token = default);

        IReadOnlyDictionary<string, string> HashGetAll([NotNull] string hashKey, CommandFlags flags = CommandFlags.None, CancellationToken token = default);

        Task<bool> HashDeleteAsync(string hashKey, string key, CommandFlags flags = CommandFlags.None, CancellationToken token = default);
    }
}
