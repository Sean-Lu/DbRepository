using Sean.Core.DbRepository.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sean.Core.DbRepository;

public class OrderByClauseSqlBuilder<TEntity> : BaseSqlBuilder<IOrderByClause<TEntity>>, IOrderByClause<TEntity>
{
    private string OrderBySql => _orderBy.IsValueCreated && _orderBy.Value.Length > 0 ? _includeKeyword ? $" ORDER BY {_orderBy.Value}" : _orderBy.Value.ToString() : string.Empty;

    private readonly Lazy<StringBuilder> _orderBy = new();

    private readonly List<Action> _orderByActions = new();

    private bool _isMultiTable;
    private bool _includeKeyword;

    private OrderByClauseSqlBuilder(DatabaseType dbType) : base(dbType, typeof(TEntity).GetEntityInfo().TableName)
    {
    }
    private OrderByClauseSqlBuilder(ISqlAdapter sqlAdapter) : base(sqlAdapter)
    {
    }

    public static IOrderByClause<TEntity> Create(ISqlAdapter sqlAdapter)
    {
        return new OrderByClauseSqlBuilder<TEntity>(sqlAdapter);
    }
    public static IOrderByClause<TEntity> Create(DatabaseType databaseType)
    {
        return new OrderByClauseSqlBuilder<TEntity>(databaseType);
    }

    #region [ORDER BY]
    /// <summary>
    /// ORDER BY column_name,column_name ASC|DESC;
    /// </summary>
    /// <param name="orderBy">The [ORDER BY] keyword is not included.</param>
    /// <returns></returns>
    public virtual IOrderByClause<TEntity> OrderBy(string orderBy)
    {
        _orderByActions.Add(() =>
        {
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                if (_orderBy.Value.Length > 0) _orderBy.Value.Append(", ");
                _orderBy.Value.Append(orderBy);
            }
        });
        return this;
    }
    public virtual IOrderByClause<TEntity> OrderBy(OrderByCondition orderBy)
    {
        orderBy?.Resolve(c =>
        {
            if (!string.IsNullOrEmpty(c.OrderBy))
            {
                OrderBy(c.OrderBy);
            }
            else
            {
                OrderBy(c.Type, c.Fields);
            }
        });
        return this;
    }
    public virtual IOrderByClause<TEntity> OrderBy(OrderByType type, params string[] fieldNames)
    {
        _orderByActions.Add(() =>
        {
            if (fieldNames != null && fieldNames.Any())
            {
                if (_orderBy.Value.Length > 0) _orderBy.Value.Append(", ");

                _orderBy.Value.Append($"{string.Join(", ", fieldNames.Select(fieldName => SqlAdapter.FormatFieldName(fieldName)).ToList())} {type.ToSqlString()}");
            }
        });
        return this;
    }
    public virtual IOrderByClause<TEntity> OrderBy(OrderByType type, Expression<Func<TEntity, object>> fieldExpression)
    {
        return OrderBy(type, fieldExpression.GetFieldNames().ToArray());
    }
    public virtual IOrderByClause<TEntity> OrderBy<TEntity2>(OrderByType type, Expression<Func<TEntity2, object>> fieldExpression)
    {
        _isMultiTable = true;
        _orderByActions.Add(() =>
        {
            var fieldNames = fieldExpression.GetFieldNames();
            if (fieldNames != null && fieldNames.Any())
            {
                if (_orderBy.Value.Length > 0) _orderBy.Value.Append(", ");

                var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
                {
                    MultiTable = true
                };
                _orderBy.Value.Append($"{string.Join(", ", fieldNames.Select(fieldName => aqlAdapter.FormatFieldName(fieldName)).ToList())} {type.ToSqlString()}");
            }
        });
        return this;
    }
    #endregion

    public IOrderByClause<TEntity> IsMultiTable(bool isMultiTable)
    {
        _isMultiTable = isMultiTable;
        return this;
    }

    public IOrderByClause<TEntity> IncludeKeyword(bool includeKeyword)
    {
        _includeKeyword = includeKeyword;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        if (_isMultiTable)
        {
            SqlAdapter.MultiTable = true;
        }

        if (_orderByActions.Any())
        {
            _orderByActions.ForEach(c => c.Invoke());
        }

        var sb = new StringBuilder();
        sb.Append(OrderBySql);

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString()
        };
        return sql;
    }
}