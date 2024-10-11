namespace Sean.Core.DbRepository;

public interface ISqlAdapter
{
    DatabaseType DbType { get; set; }
    string TableName { get; set; }
    string AliasName { get; set; }
    bool MultiTable { get; set; }

    string FormatTableName(string tableName);

    string FormatFieldName(string fieldName, string tableName = null, string aliasName = null);

    string FormatSqlParameter(string parameter);
}