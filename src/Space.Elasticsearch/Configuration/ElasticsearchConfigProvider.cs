using System.Threading.Tasks;

namespace Space.ElasticSearch.Configuration
{
    /// <summary>
    /// ElasticSearch 配置提供程序
    /// </summary>
    public class ElasticSearchConfigProvider : IElasticSearchConfigProvider
    {
        /// <summary>
        /// 配置
        /// </summary>
        private readonly ElasticSearchOptions _options;

        /// <summary>
        /// 初始化一个<see cref="ElasticSearchConfigProvider"/>类型的实例
        /// </summary>
        /// <param name="options">ElasticSearch 连接配置</param>
        public ElasticSearchConfigProvider(ElasticSearchOptions options) => _options = options;

        public Task<ElasticSearchOptions> GetConfigAsync() => Task.FromResult(_options);
    }
}
