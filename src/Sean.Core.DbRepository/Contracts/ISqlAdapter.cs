namespace Sean.Core.DbRepository
{
    public interface ISqlAdapter
    {
        DatabaseType DbType { get; }

        string FormatTableName(string tableName);
        string FormatFieldName(string fieldName);
        string FormatInputParameter(string parameter);

        string GetSqlForSelectLastInsertId();
    }
}

