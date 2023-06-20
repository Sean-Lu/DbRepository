using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository;

public class DefaultSqlAdapter : ISqlAdapter
{
    public DatabaseType DbType { get; set; }
    public string TableName { get; set; }
    public bool MultiTable { get; set; }

    public DefaultSqlAdapter(DatabaseType dbType, string tableName)
    {
        DbType = dbType;
        TableName = tableName;
    }

    public virtual string FormatTableName()
    {
        return FormatTableName(TableName);
    }
    public virtual string FormatTableName(string tableName)
    {
        return DbType.MarkAsTableOrFieldName(tableName);
    }

    public string FormatFieldName(string fieldName)
    {
        return FormatFieldName(fieldName, false);
    }

    public virtual string FormatFieldName(string fieldName, bool multiTable)
    {
        if (multiTable || MultiTable)
        {
            return FormatFieldName(fieldName, TableName);
        }

        return DbType.MarkAsTableOrFieldName(fieldName);
    }

    public virtual string FormatFieldName(string fieldName, string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return FormatFieldName(fieldName);
        }

        return $"{FormatTableName(tableName)}.{FormatFieldName(fieldName)}";
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
            TableName = typeof(TEntity).GetMainTableName();
        }
    }
}