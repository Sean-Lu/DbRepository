namespace Sean.Core.DbRepository
{
    public interface ISqlAdapter
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        DatabaseType DbType { get; set; }
        /// <summary>
        /// 表名
        /// </summary>
        string TableName { get; set; }
        /// <summary>
        /// 是否多表
        /// </summary>
        bool MultiTable { get; set; }

        string FormatTableName();
        string FormatTableName(string tableName);

        string FormatFieldName(string fieldName);
        string FormatFieldName(string fieldName, bool multiTable);
        string FormatFieldName(string fieldName, string tableName);

        string FormatInputParameter(string parameter);

        /// <summary>
        /// SQL语句：获取上一次生成的自增id
        /// </summary>
        /// <returns></returns>
        string GetSqlForSelectLastInsertId();
    }
}

