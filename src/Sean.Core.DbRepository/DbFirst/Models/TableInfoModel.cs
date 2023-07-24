using System;

namespace Sean.Core.DbRepository.DbFirst;

public class TableInfoModel
{
    public string TableSchema { get; set; }
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; }
    /// <summary>
    /// 表注释
    /// </summary>
    public string TableComment { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }
}