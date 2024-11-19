namespace Sean.Core.DbRepository;

public abstract class BaseSqlBuilder<TBuild> : IBaseSqlBuilder<TBuild> where TBuild : class
{
    protected string TableName => SqlAdapter.TableName;
    /// <summary>
    /// SQL adapter.
    /// </summary>
    protected ISqlAdapter SqlAdapter { get; }
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

    public virtual TBuild SetTableName(string tableName)
    {
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            SqlAdapter.TableName = tableName;
        }
        return this as TBuild;
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
            else
            {

                switch (SqlAdapter.DbType)
                {
                    case DatabaseType.MsAccess:
                        {
                            sqlCommand.ConvertParameterToDictionaryByPosition(false);
                            break;
                        }
                    case DatabaseType.Informix:// Informix ODBC Driver.
                    case DatabaseType.DuckDB:
                    case DatabaseType.Xugu:
                        {
                            sqlCommand.ConvertParameterToDictionaryByPosition(true);
                            break;
                        }
                }
            }
        }

        return sqlCommand;
    }

    protected abstract ISqlCommand BuildSqlCommand();
}