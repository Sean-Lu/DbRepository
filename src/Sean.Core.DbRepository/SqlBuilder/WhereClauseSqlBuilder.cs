using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class WhereClauseSqlBuilder<TEntity> : BaseSqlBuilder<IWhereClause<TEntity>>, IWhereClause<TEntity>
{
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $"{_where.Value}" : string.Empty;

    private readonly Lazy<StringBuilder> _where = new();

    private readonly List<Action> _whereActions = new();

    private bool _isMultiTable;

    private object _parameter;

    private WhereClauseSqlBuilder(ISqlAdapter sqlAdapter) : base(sqlAdapter)
    {
    }
    private WhereClauseSqlBuilder(DatabaseType databaseType) : base(databaseType, typeof(TEntity).GetEntityInfo().TableName)
    {
    }

    public static IWhereClause<TEntity> Create(ISqlAdapter sqlAdapter)
    {
        return new WhereClauseSqlBuilder<TEntity>(sqlAdapter);
    }
    public static IWhereClause<TEntity> Create(DatabaseType databaseType)
    {
        return new WhereClauseSqlBuilder<TEntity>(databaseType);
    }

    #region [WHERE]
    public virtual IWhereClause<TEntity> Where(string where)
    {
        _whereActions.Add(() => SqlBuilderUtil.Where(_where.Value, where));
        return this;
    }

    public virtual IWhereClause<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IWhereClause<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
    {
        _isMultiTable = true;
        _whereActions.Add(() =>
        {
            var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
            {
                MultiTable = true
            };
            SqlBuilderUtil.Where(aqlAdapter,
                SqlParameterUtil.ConvertToDicParameter(_parameter),
                whereClause => SqlBuilderUtil.Where(_where.Value, whereClause),
                dicParameters => SetParameter(dicParameters),
                whereExpression);
        });
        return this;
    }

    public virtual IWhereClause<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }

    public virtual IWhereClause<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return Where(condition ? trueWhereExpression : falseWhereExpression);
    }

    public virtual IWhereClause<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }

    public virtual IWhereClause<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity2, bool>> falseWhereExpression)
    {
        return Where(condition ? trueWhereExpression : falseWhereExpression);
    }

    public virtual IWhereClause<TEntity> WhereIF<TEntity2, TEntity3>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity3, bool>> falseWhereExpression)
    {
        return condition ? Where(trueWhereExpression) : Where(falseWhereExpression);
    }

    public virtual IWhereClause<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        _whereActions.Add(() =>
        {
            SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        });
        return this;
    }
    public virtual IWhereClause<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        _isMultiTable = true;
        _whereActions.Add(() =>
        {
            var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
            {
                MultiTable = true
            };
            SqlBuilderUtil.WhereField(aqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        });
        return this;
    }
    #endregion

    public virtual IWhereClause<TEntity> SetParameter(object param)
    {
        _parameter = param;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        if (_isMultiTable)
        {
            SqlAdapter.MultiTable = true;
        }

        if (_whereActions.Any())
        {
            _whereActions.ForEach(c => c.Invoke());
            _whereActions.Clear();
        }

        var sb = new StringBuilder();
        sb.Append(WhereSql);

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}