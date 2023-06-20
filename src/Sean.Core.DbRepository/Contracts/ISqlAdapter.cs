namespace Sean.Core.DbRepository;

public interface ISqlAdapter
{
    DatabaseType DbType { get; set; }
    string TableName { get; set; }
    bool MultiTable { get; set; }

    string FormatTableName();
    string FormatTableName(string tableName);

    string FormatFieldName(string fieldName);
    string FormatFieldName(string fieldName, bool multiTable);
    string FormatFieldName(string fieldName, string tableName);

    string FormatSqlParameter(string parameter);
}