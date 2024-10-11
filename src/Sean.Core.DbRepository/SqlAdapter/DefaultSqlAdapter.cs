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

    public virtual string FormatTableName(string tableName)
    {
        return DbType.MarkAsTableOrFieldName(tableName);
    }

    public string FormatFieldName(string fieldName, string tableName = null, string aliasName = null)
    {
        if (!string.IsNullOrWhiteSpace(aliasName))
        {
            return $"{aliasName}.{DbType.MarkAsTableOrFieldName(fieldName)}";
        }
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            return $"{DbType.MarkAsTableOrFieldName(tableName)}.{DbType.MarkAsTableOrFieldName(fieldName)}";
        }
        if (MultiTable)
        {
            return !string.IsNullOrWhiteSpace(AliasName) 
                ? $"{AliasName}.{DbType.MarkAsTableOrFieldName(fieldName)}" 
                : $"{DbType.MarkAsTableOrFieldName(TableName)}.{DbType.MarkAsTableOrFieldName(fieldName)}";
        }
        return DbType.MarkAsTableOrFieldName(fieldName);
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