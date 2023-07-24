namespace Sean.Core.DbRepository.DbFirst;

public class TableFieldReferenceModel
{
    /// <summary>
    /// 外键名称
    /// </summary>
    public string ForeignKeyName { get; set; }

    public string TableSchema { get; set; }
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; }
    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; }

    public string ReferencedTableSchema { get; set; }
    /// <summary>
    /// 关联的表名
    /// </summary>
    public string ReferencedTableName { get; set; }
    /// <summary>
    /// 关联的表字段名
    /// </summary>
    public string ReferencedFieldName { get; set; }
}