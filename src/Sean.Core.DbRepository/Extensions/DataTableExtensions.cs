using System.Collections.Generic;
using System.Data;

namespace Sean.Core.DbRepository.Extensions
{
    /// <summary>
    /// Extensions for <see cref="DataTable"/>
    /// </summary>
    public static class DataTableExtensions
    {
        /// <summary>
        /// 将<see cref="DataTable"/>转换成实体列表
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable dt, bool ignoreCase = false)
        {
            if (dt == null)
            {
                return default;
            }

            var list = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                var item = row.ToEntity<T>(ignoreCase);
                list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// 将<see cref="DataTable"/>转换成实体（默认取第1个）
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns></returns>
        public static T ToEntity<T>(this DataTable dt, bool ignoreCase = false)
        {
            if (dt == null)
            {
                return default;
            }

            foreach (DataRow row in dt.Rows)
            {
                return row.ToEntity<T>(ignoreCase);
            }

            return default;
        }
    }
}
