namespace Sean.Core.DbRepository;

public interface ISqlAdapter
{
    DatabaseType DbType { get; set; }
    string TableName { get; set; }
    string AliasName { get; set; }
    bool MultiTable { get; set; }

    string FormatTableName();
    string FormatTableName(string tableName, string aliasName = null);

    string FormatFieldName(string fieldName);
    string FormatFieldName(string fieldName, string tableName, string aliasName = null);

    string FormatSqlParameter(string parameter);
}