namespace Sean.Core.DbRepository
{
    public interface ISqlAdapter
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        DatabaseType DbType { get; }

        string FormatTableName(string tableName);
        string FormatFieldName(string fieldName);
        string FormatInputParameter(string parameter);

        /// <summary>
        /// SQL语句：获取上一次生成的自增id
        /// </summary>
        /// <returns></returns>
        string GetSqlForSelectLastInsertId();
    }
}

