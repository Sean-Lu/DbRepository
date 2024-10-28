using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class CountableSqlBuilder<TEntity> : BaseSqlBuilder<ICountable<TEntity>>, ICountable<TEntity>
{
    private const string SqlTemplate = "SELECT COUNT(*) FROM {0}{1}";

    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();
    private readonly Lazy<StringBuilder> _where = new();

    private readonly List<Action> _whereActions = new();

    private bool MultiTable => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

    private object _parameter;

    private CountableSqlBuilder(DatabaseType dbType) : base(dbType, typeof(TEntity).GetEntityInfo().TableName)
    {
    }

    /// <summary>
    /// Create an instance of <see cref="ICountable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <returns></returns>
    public static ICountable<TEntity> Create(DatabaseType dbType)
    {
        var sqlBuilder = new CountableSqlBuilder<TEntity>(dbType);
        return sqlBuilder;
    }

    #region [Join Table]
    public virtual ICountable<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ICountable<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ICountable<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual ICountable<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual ICountable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual ICountable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual ICountable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual ICountable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }

    public virtual ICountable<TEntity> InnerJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual ICountable<TEntity> LeftJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual ICountable<TEntity> RightJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual ICountable<TEntity> FullJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    #endregion

    #region [WHERE]
    public virtual ICountable<TEntity> Where(string where)
    {
        _whereActions.Add(() => SqlBuilderUtil.Where(_where.Value, where));
        return this;
    }

    public virtual ICountable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
    {
        _whereActions.Add(() =>
        {
            SqlBuilderUtil.Where(SqlAdapter,
                SqlParameterUtil.ConvertToDicParameter(_parameter),
                whereClause => SqlBuilderUtil.Where(_where.Value, whereClause),
                dicParameters => SetParameter(dicParameters),
                whereExpression);
        });
        return this;
    }
    public virtual ICountable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
    {
        _whereActions.Add(() =>
        {
            var sqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
            {
                AliasName = aliasName,
                MultiTable = true
            };
            SqlBuilderUtil.Where(sqlAdapter,
                SqlParameterUtil.ConvertToDicParameter(_parameter),
                whereClause => SqlBuilderUtil.Where(_where.Value, whereClause),
                dicParameters => SetParameter(dicParameters),
                whereExpression);
        });
        return this;
    }

    public virtual ICountable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }

    public virtual ICountable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return Where(condition ? trueWhereExpression : falseWhereExpression);
    }

    public virtual ICountable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
    {
        return condition ? Where(whereExpression, aliasName) : this;
    }

    public virtual ICountable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity2, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual ICountable<TEntity> WhereIF<TEntity2, TEntity3>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity3, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual ICountable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        _whereActions.Add(() =>
        {
            SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        });
        return this;
    }
    public virtual ICountable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null, string aliasName = null)
    {
        _whereActions.Add(() =>
        {
            var sqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
            {
                AliasName = aliasName,
                MultiTable = true
            };
            SqlBuilderUtil.WhereField(sqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        });
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
        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }

        if (_whereActions.Any())
        {
            _whereActions.ForEach(c => c.Invoke());
            _whereActions.Clear();
        }

        var sb = new StringBuilder();
        sb.Append(string.Format(SqlTemplate, $"{SqlAdapter.FormatTableName(TableName)}{JoinTableSql}", WhereSql));

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}