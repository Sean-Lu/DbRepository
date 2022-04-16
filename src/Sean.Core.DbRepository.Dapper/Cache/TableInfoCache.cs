using System;
using System.Collections.Concurrent;

namespace Sean.Core.DbRepository.Dapper
{
    /// <summary>
    /// 表信息缓存
    /// </summary>
    internal class TableInfoCache
    {
        private static readonly ConcurrentDictionary<string, bool> _tableExistsDic = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// 查询指定的表是否存在
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        public static bool IsTableExists(string tableName)
        {
            if (!string.IsNullOrWhiteSpace(tableName) && _tableExistsDic.TryGetValue(tableName, out var tableExists) && tableExists)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 设置指定的表是否存在
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="exist"></param>
        public static void IsTableExists(string tableName, bool exist)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));
            _tableExistsDic.AddOrUpdate(tableName, exist, (_, _) => exist);
        }
    }
}
