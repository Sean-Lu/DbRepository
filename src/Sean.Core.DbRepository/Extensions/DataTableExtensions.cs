using System.Collections.Generic;
using System.Data;

namespace Sean.Core.DbRepository.Extensions;

/// <summary>
/// Extensions for <see cref="DataTable"/>
/// </summary>
public static class DataTableExtensions
{
    /// <summary>
    /// 将<see cref="DataTable"/>转换成实体列表
    /// </summary>
    /// <param name="dt">数据表</param>
    /// <returns></returns>
    public static List<T> ToList<T>(this DataTable dt)
    {
        if (dt == null)
        {
            return default;
        }

        var list = new List<T>();
        if (dt.Rows.Count == 0)
        {
            return list;
        }

        var mapper = DataRowExtensions.CreateMapper<T>(dt);
        foreach (DataRow row in dt.Rows)
        {
            var item = mapper(row);
            list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// 将<see cref="DataTable"/>转换成实体（默认取第1个）
    /// </summary>
    /// <param name="dt">数据表</param>
    /// <returns></returns>
    public static T ToEntity<T>(this DataTable dt)
    {
        if (dt == null)
        {
            return default;
        }

        if (dt.Rows.Count > 0)
        {
            var mapper = DataRowExtensions.CreateMapper<T>(dt);
            return mapper(dt.Rows[0]);
        }

        return default;
    }
}
