﻿using System.Collections.Generic;

namespace Space.ElasticSearch.Model
{
    /// <summary>
    /// 查询结果
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IQueryResult<T>
    {
        /// <summary>
        /// 总行数
        /// </summary>
        long TotalCount { get; set; }

        /// <summary>
        /// 查询占用时间
        /// </summary>
        long Took { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        IEnumerable<T> Data { get; }
    }

    /// <summary>
    /// 自定义查询结果
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class CustomQueryResult<T> : IQueryResult<T>
    {
        /// <summary>
        /// 总行数
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// 查询占用时间
        /// </summary>
        public long Took { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public IEnumerable<T> Data { get; set; }
    }
}
