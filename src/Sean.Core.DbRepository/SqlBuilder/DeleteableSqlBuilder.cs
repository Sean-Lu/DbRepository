using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class DeleteableSqlBuilder<TEntity> : BaseSqlBuilder<IDeleteable<TEntity>>, IDeleteable<TEntity>
{
    private const string SqlTemplate = "DELETE FROM {0}{1}";

    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();
    private readonly Lazy<StringBuilder> _where = new();

    private readonly List<Action> _whereActions = new();

    private bool MultiTable => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

    private bool _allowEmptyWhereClause;
    private object _parameter;

    private DeleteableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
    {
    }

    /// <summary>
    /// Create an instance of <see cref="IDeleteable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    public static IDeleteable<TEntity> Create(DatabaseType dbType, string tableName = null)
    {
        var sqlBuilder = new DeleteableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetEntityInfo().TableName);
        return sqlBuilder;
    }

    #region [Join Table]
    public virtual IDeleteable<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IDeleteable<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IDeleteable<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IDeleteable<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual IDeleteable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2, string aliasName = null)
    {
        var joinTableName = typeof(TEntity2).GetEntityInfo().TableName;
        return InnerJoin($"{SqlAdapter.FormatTableName(joinTableName)}{(!string.IsNullOrWhiteSpace(aliasName) ? $" {aliasName}" : string.Empty)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, aliasName)}");
    }
    public virtual IDeleteable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2, string aliasName = null)
    {
        var joinTableName = typeof(TEntity2).GetEntityInfo().TableName;
        return LeftJoin($"{SqlAdapter.FormatTableName(joinTableName)}{(!string.IsNullOrWhiteSpace(aliasName) ? $" {aliasName}" : string.Empty)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, aliasName)}");
    }
    public virtual IDeleteable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2, string aliasName = null)
    {
        var joinTableName = typeof(TEntity2).GetEntityInfo().TableName;
        return RightJoin($"{SqlAdapter.FormatTableName(joinTableName)}{(!string.IsNullOrWhiteSpace(aliasName) ? $" {aliasName}" : string.Empty)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, aliasName)}");
    }
    public virtual IDeleteable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2, string aliasName = null)
    {
        var joinTableName = typeof(TEntity2).GetEntityInfo().TableName;
        return FullJoin($"{SqlAdapter.FormatTableName(joinTableName)}{(!string.IsNullOrWhiteSpace(aliasName) ? $" {aliasName}" : string.Empty)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, aliasName)}");
    }
    #endregion

    #region [WHERE]
    public virtual IDeleteable<TEntity> Where(string where)
    {
        _whereActions.Add(() => SqlBuilderUtil.Where(_where.Value, where));
        return this;
    }

    public virtual IDeleteable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IDeleteable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
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

    public virtual IDeleteable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }

    public virtual IDeleteable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return Where(condition ? trueWhereExpression : falseWhereExpression);
    }

    public virtual IDeleteable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
    {
        return condition ? Where(whereExpression, aliasName) : this;
    }

    public virtual IDeleteable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity2, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual IDeleteable<TEntity> WhereIF<TEntity2, TEntity3>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity3, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual IDeleteable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        _whereActions.Add(() =>
        {
            SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        });
        return this;
    }
    public virtual IDeleteable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null, string aliasName = null)
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

    public virtual IDeleteable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true)
    {
        _allowEmptyWhereClause = allowEmptyWhereClause;
        return this;
    }

    public virtual IDeleteable<TEntity> SetParameter(object param)
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

        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
        {
            // Set the primary key field to the [where] filtering condition.
            foreach (var fieldInfo in typeof(TEntity).GetEntityInfo().FieldInfos.Where(c => c.IsPrimaryKey).ToList())
            {
                var fieldName = fieldInfo.FieldName;
                var parameterName = fieldInfo.Property?.Name ?? fieldName;
                SqlBuilderUtil.WhereField<TEntity>(SqlAdapter, _where.Value, entity => fieldName, SqlOperation.Equal, paramName: parameterName);
            }
        }

        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

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