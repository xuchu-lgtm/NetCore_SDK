using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nest;
using Space.ElasticSearch.Abstractions;
using Space.ElasticSearch.Configuration;
using Space.ElasticSearch.Implementations;

namespace Space.ElasticSearch.Extensions
{
    /// <summary>
    /// ElasticSearch 扩展
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// 注册ElasticSearch日志操作
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="setupAction">配置操作</param>
        public static void AddElasticSearch(this IServiceCollection services, Action<ElasticSearchOptions> setupAction)
        {
            var config = new ElasticSearchOptions();
            setupAction?.Invoke(config);
            services.TryAddSingleton<IElasticSearchConfigProvider>(new ElasticSearchConfigProvider(config));
            services.TryAddScoped<IElasticSearchClient, ElasticSearchClient>();
        }

        /// <summary>
        /// 注册ElasticSearch日志操作
        /// </summary>
        /// <typeparam name="TElasticSearchConfigProvider">ElasticSearch配置提供器</typeparam>
        /// <param name="services">服务集合</param>
        public static void AddElasticSearch<TElasticSearchConfigProvider>(this IServiceCollection services)
            where TElasticSearchConfigProvider : class, IElasticSearchConfigProvider
        {
            services.TryAddScoped<IElasticSearchConfigProvider, TElasticSearchConfigProvider>();
            services.TryAddScoped<IElasticSearchClient, ElasticSearchClient>();
        }
    }

    /// <summary>
    /// ES客户端(<see cref="IElasticClient"/>) 扩展
    /// </summary>
    internal static class ElasticClientExtensions
    {
        /// <summary>
        /// 初始化索引映射
        /// </summary>
        /// <param name="client">ES客户端</param>
        /// <param name="indexName">索引名</param>
        public static async Task InitializeIndexMapAsync(this IElasticClient client, string indexName)
        {
            var newName = indexName + DateTime.Now.Ticks;
            var result = await client.CreateIndexAsync(newName,
                x => x.Index(newName)
                    .Settings(o =>
                        o.NumberOfShards(1)
                            .NumberOfReplicas(1)
                            .Setting("max_result_window", int.MaxValue)));
            if (result.Acknowledged)
            {
                await client.AliasAsync(x => x.Add(o => o.Index(newName).Alias(indexName)));
                return;
            }
            throw new ElasticSearchException($"创建索引 {indexName} 失败：{result.ServerError.Error.Reason}");
        }

        /// <summary>
        /// 初始化索引映射
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="client">ES客户端</param>
        /// <param name="indexName">索引名</param>
        public static async Task InitializeIndexMapAsync<T>(this IElasticClient client, string indexName) where T : class
        {
            var newName = indexName + DateTime.Now.Ticks;
            var result = await client.CreateIndexAsync(newName,
                x => x.Index(newName)
                    .Settings(o =>
                        o.NumberOfShards(1)
                            .NumberOfReplicas(1)
                            .Setting("max_result_window", int.MaxValue))
                    .Mappings(m => m.Map<T>(mm => mm.AutoMap())));
            if (result.Acknowledged)
            {
                await client.AliasAsync(x => x.Add(o => o.Index(newName).Alias(indexName)));
                return;
            }
            throw new ElasticSearchException($"创建索引 {indexName} 失败：{result.ServerError.Error.Reason}");
        }

        /// <summary>
        /// 初始化索引映射
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="client">ES客户端</param>
        /// <param name="indexName">索引名</param>
        /// <param name="numberOfShards">分片数</param>
        /// <param name="numberOfReplicas">副本数</param>
        public static async Task InitializeIndexMapAsync<T>(this IElasticClient client, string indexName, int numberOfShards,
            int numberOfReplicas) where T : class
        {
            var newName = indexName + DateTime.Now.Ticks;
            var result = await client.CreateIndexAsync(newName,
                x => x.Index(newName)
                    .Settings(o =>
                        o.NumberOfShards(numberOfShards)
                            .NumberOfReplicas(numberOfReplicas)
                            .Setting("max_result_window", int.MaxValue))
                    .Mappings(m => m.Map<T>(mm => mm.AutoMap())));
            if (result.Acknowledged)
            {
                await client.AliasAsync(x => x.Add(o => o.Index(newName).Alias(indexName)));
                return;
            }
            throw new ElasticSearchException($"创建索引 {indexName} 失败：{result.ServerError.Error.Reason}");
        }

        /// <summary>
        /// 初始化索引映射
        /// </summary>
        /// <typeparam name="T1">实体类型</typeparam>
        /// <typeparam name="T2">实体类型</typeparam>
        /// <param name="client">ES客户端</param>
        /// <param name="indexName">索引名</param>
        public static async Task InitializeIndexMapAsync<T1, T2>(this IElasticClient client, string indexName)
            where T1 : class where T2 : class
        {

        }
    }
}
