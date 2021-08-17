using System.Collections.Generic;

namespace Space.ElasticSearch.Configuration
{
    /// <summary>
    /// ES 连接配置
    /// </summary>
    public class ElasticSearchOptions
    {
        /// <summary>
        /// 节点列表
        /// </summary>
        public IEnumerable<ElasticSearchNode> Nodes { get; set; }

        /// <summary>
        /// 连接池类型
        /// </summary>
        public ElasticSearchConnectionPoolType PoolType { get; set; } = ElasticSearchConnectionPoolType.Static;

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 显示调试信息
        /// </summary>
        public bool DisableDebugInfo { get; set; } = true;

        /// <summary>
        /// 抛出异常。默认false，错误信息在每个操作的response中
        /// </summary>
        public bool ThrowExceptions { get; set; } = false;

        /// <summary>
        /// 是否禁用Ping。禁用ping 第一次使用节点或使用被标记死亡的节点进行ping
        /// </summary>
        public bool DisablePing { get; set; } = true;
    }
}
