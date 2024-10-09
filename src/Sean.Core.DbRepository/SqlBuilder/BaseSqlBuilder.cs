namespace Sean.Core.DbRepository;

public abstract class BaseSqlBuilder<TBuild> : IBaseSqlBuilder<TBuild> where TBuild : class
{
    /// <summary>
    /// SQL adapter.
    /// </summary>
    public ISqlAdapter SqlAdapter { get; }
    /// <summary>
    /// Whether the SQL is indent. The default value is false.
    /// </summary>
    public bool SqlIndented { get; set; } = DbContextConfiguration.Options.SqlIndented;
    /// <summary>
    /// Whether the SQL is parameterized. The default value is true.
    /// </summary>
    public bool SqlParameterized { get; set; } = DbContextConfiguration.Options.SqlParameterized;

    protected BaseSqlBuilder(DatabaseType dbType, string tableName)
    {
        SqlAdapter = new DefaultSqlAdapter(dbType, tableName);
    }
    protected BaseSqlBuilder(ISqlAdapter sqlAdapter)
    {
        SqlAdapter = sqlAdapter;
    }

    public virtual TBuild SetSqlIndented(bool sqlIndented)
    {
        SqlIndented = sqlIndented;
        return this as TBuild;
    }
    public virtual TBuild SetSqlParameterized(bool sqlParameterized)
    {
        SqlParameterized = sqlParameterized;
        return this as TBuild;
    }

    public virtual ISqlCommand Build()
    {
        var sqlCommand = BuildSqlCommand();

        if (sqlCommand?.Parameter != null)
        {
            if (!SqlParameterized)
            {
                sqlCommand.ConvertSqlToNonParameter();
            }
            else if (SqlAdapter.DbType is DatabaseType.MsAccess or DatabaseType.Informix or DatabaseType.DuckDB or DatabaseType.Xugu)
            {
                sqlCommand.BindSqlParameterType = BindSqlParameterType.BindByPosition;
                sqlCommand.ConvertParameterToDictionary();
                // Informix ODBC Driver.
                if (SqlAdapter.DbType is DatabaseType.Informix or DatabaseType.DuckDB or DatabaseType.Xugu)
                {
                    sqlCommand.ConvertSqlToUseQuestionMarkParameter();
                }
            }
        }

        return sqlCommand;
    }

    protected abstract ISqlCommand BuildSqlCommand();
}