using System.Threading.Tasks;

namespace Space.ElasticSearch.Configuration
{
    /// <summary>
    /// ElasticSearch 配置提供程序
    /// </summary>
    public interface IElasticSearchConfigProvider
    {
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <returns></returns>
        Task<ElasticSearchOptions> GetConfigAsync();
    }
}
