using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository;

public class DefaultSqlAdapter : ISqlAdapter
{
    public DatabaseType DbType { get; set; }
    public string TableName { get; set; }
    public string AliasName { get; set; }
    public bool MultiTable { get; set; }

    public DefaultSqlAdapter(DatabaseType dbType, string tableName)
    {
        DbType = dbType;
        TableName = tableName;
    }

    public virtual string FormatTableName()
    {
        return !string.IsNullOrWhiteSpace(AliasName) ?
            $"{DbType.MarkAsIdentifier(TableName)} {AliasName}"
            : DbType.MarkAsIdentifier(TableName);
    }
    public virtual string FormatTableName(string tableName, string aliasName = null)
    {
        return !string.IsNullOrWhiteSpace(aliasName) ?
            $"{DbType.MarkAsIdentifier(tableName)} {aliasName}"
            : DbType.MarkAsIdentifier(tableName);
    }

    public string FormatFieldName(string fieldName)
    {
        if (MultiTable)
        {
            return !string.IsNullOrWhiteSpace(AliasName)
                ? $"{AliasName}.{DbType.MarkAsIdentifier(fieldName)}"
                : $"{DbType.MarkAsIdentifier(TableName)}.{DbType.MarkAsIdentifier(fieldName)}";
        }
        return DbType.MarkAsIdentifier(fieldName);
    }
    public string FormatFieldName(string fieldName, string tableName, string aliasName = null)
    {
        if (!string.IsNullOrWhiteSpace(aliasName))
        {
            return $"{aliasName}.{DbType.MarkAsIdentifier(fieldName)}";
        }
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            return $"{DbType.MarkAsIdentifier(tableName)}.{DbType.MarkAsIdentifier(fieldName)}";
        }
        return DbType.MarkAsIdentifier(fieldName);
    }

    public virtual string FormatSqlParameter(string parameter)
    {
        return DbType.MarkAsSqlParameter(parameter);
    }
}

public class DefaultSqlAdapter<TEntity> : DefaultSqlAdapter
{
    public DefaultSqlAdapter(DatabaseType dbType, string tableName = null) : base(dbType, tableName)
    {
        if (string.IsNullOrEmpty(TableName))
        {
            TableName = typeof(TEntity).GetEntityInfo().TableName;
        }
    }
}