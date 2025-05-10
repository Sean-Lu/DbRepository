namespace Sean.Core.DbRepository;

public abstract class BaseSqlBuilder<TEntity, TBuild> : IBaseSqlBuilder<TBuild> where TBuild : class
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

    protected const string MainTableDefaultAlias = "t_";

    protected BaseSqlBuilder(DatabaseType dbType)
    {
        SqlAdapter = new DefaultSqlAdapter<TEntity>(dbType);
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
                    //case DatabaseType.DuckDB:// √
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