using System;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class CountableSqlBuilder<TEntity> : BaseSqlBuilder, ICountable<TEntity>
{
    private const string SqlTemplate = "SELECT COUNT(*) FROM {0}{1}";

    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();
    private readonly Lazy<StringBuilder> _where = new();

    private bool MultiTable => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

    private object _parameter;

    private CountableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
    {
    }

    /// <summary>
    /// Create an instance of <see cref="ICountable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    public static ICountable<TEntity> Create(DatabaseType dbType, string tableName = null)
    {
        var sqlBuilder = new CountableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
        return sqlBuilder;
    }

    #region [Join Table]
    public virtual ICountable<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ICountable<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ICountable<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ICountable<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual ICountable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return InnerJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual ICountable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return LeftJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual ICountable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return RightJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual ICountable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return FullJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    #endregion

    #region [WHERE]
    public virtual ICountable<TEntity> Where(string where)
    {
        SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.None, where);
        return this;
    }
    public virtual ICountable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
    {
        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }
        SqlBuilderUtil.Where(SqlAdapter,
            SqlParameterUtil.ConvertToDicParameter(_parameter),
            whereClause => Where(whereClause),
            dicParameters => SetParameter(dicParameters),
            whereExpression);
        return this;
    }
    public virtual ICountable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
    {
        var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
        {
            MultiTable = true
        };
        SqlBuilderUtil.Where(aqlAdapter,
            SqlParameterUtil.ConvertToDicParameter(_parameter),
            whereClause => Where(whereClause),
            dicParameters => SetParameter(dicParameters),
            whereExpression);
        return this;
    }

    public virtual ICountable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }
        SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        return this;
    }
    public virtual ICountable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
        {
            MultiTable = true
        };
        SqlBuilderUtil.WhereField(aqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        return this;
    }

    public virtual ICountable<TEntity> AndWhere(string where)
    {
        SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.And, where);
        return this;
    }
    public virtual ICountable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression)
    {
        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }
        SqlBuilderUtil.Where(SqlAdapter,
            SqlParameterUtil.ConvertToDicParameter(_parameter),
            whereClause => AndWhere(whereClause),
            dicParameters => SetParameter(dicParameters),
            whereExpression);
        return this;
    }
    public virtual ICountable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
    {
        var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
        {
            MultiTable = true
        };
        SqlBuilderUtil.Where(aqlAdapter,
            SqlParameterUtil.ConvertToDicParameter(_parameter),
            whereClause => AndWhere(whereClause),
            dicParameters => SetParameter(dicParameters),
            whereExpression);
        return this;
    }
    #endregion

    public virtual ICountable<TEntity> SetParameter(object param)
    {
        _parameter = param;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        var sb = new StringBuilder();
        sb.Append(string.Format(SqlTemplate, $"{SqlAdapter.FormatTableName()}{JoinTableSql}", WhereSql));

        var sql = new DefaultSqlCommand
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}