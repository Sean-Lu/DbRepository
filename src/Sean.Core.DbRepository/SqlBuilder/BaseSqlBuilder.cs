using System;

namespace Sean.Core.DbRepository;

public abstract class BaseSqlBuilder : IBaseSqlBuilder
{
    /// <summary>
    /// SQL adapter.
    /// </summary>
    public ISqlAdapter SqlAdapter { get; }
    /// <summary>
    /// Whether the SQL is indent. The default value is false.
    /// </summary>
    public bool SqlIndented { get; } = false;
    /// <summary>
    /// Whether the SQL is parameterized. The default value is true.
    /// </summary>
    public bool SqlParameterized { get; } = true;

    protected BaseSqlBuilder(DatabaseType dbType, string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        SqlAdapter = new DefaultSqlAdapter(dbType, tableName);
    }

    public ISqlCommand Build()
    {
        var sqlCommand = BuildSqlCommand();
        if (sqlCommand?.Parameter != null && SqlAdapter.DbType is DatabaseType.MsAccess or DatabaseType.Informix)
        {
            sqlCommand.BindSqlParameterType = BindSqlParameterType.BindByPosition;
            sqlCommand.ConvertParameterToDictionary();
            if (SqlAdapter.DbType is DatabaseType.Informix)// Informix ODBC Driver.
            {
                sqlCommand.ConvertSqlToUseQuestionMarkParameter();
            }
        }
        return sqlCommand;
    }

    protected virtual ISqlCommand BuildSqlCommand()
    {
        return null;
    }
}