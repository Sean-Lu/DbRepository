namespace Sean.Core.DbRepository.DbFirst;

public class TableFieldModel
{
    public string TableSchema { get; set; }
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; }
    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; }
    /// <summary>
    /// 字段描述
    /// </summary>
    public string FieldComment { get; set; }
    /// <summary>
    /// 字段默认值
    /// </summary>
    public string FieldDefault { get; set; }
    /// <summary>
    /// 字段数据类型
    /// </summary>
    public string FieldType { get; set; }
    /// <summary>
    /// 表示数字中的有效位数。从左边第一个不为0的数算起，小数点和负号不计入有效位数。
    /// </summary>
    public int? NumericPrecision { get; set; }
    /// <summary>
    /// 表示精确到多少位。大于零时，表示数字精确到小数点右边的位数；小于零时，将把该数字取舍到小数点左边的指定位数。
    /// </summary>
    public int? NumericScale { get; set; }
    /// <summary>
    /// 字符串最大长度
    /// </summary>
    public int? StringMaxLength { get; set; }
    /// <summary>
    /// 是否允许为空
    /// </summary>
    public bool? IsNullable { get; set; }
    /// <summary>
    /// 是否主键
    /// </summary>
    public bool? IsPrimaryKey { get; set; }
    /// <summary>
    /// 是否外键
    /// </summary>
    public bool? IsForeignKey { get; set; }
    /// <summary>
    /// 是否自增
    /// </summary>
    public bool? IsAutoIncrement { get; set; }
}