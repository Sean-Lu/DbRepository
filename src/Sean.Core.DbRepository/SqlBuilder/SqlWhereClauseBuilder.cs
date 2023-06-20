using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class SqlWhereClauseBuilder<TEntity>
{
    public ISqlAdapter SqlAdapter => _sqlAdapter;
    public IDictionary<string, object> Parameter => _parameter;

    private readonly ISqlAdapter _sqlAdapter;
    private readonly IDictionary<string, object> _parameter;
    private readonly List<string> _whereClauseList;

    private SqlWhereClauseBuilder(ISqlAdapter sqlAdapter, TEntity entity = default)
    {
        _sqlAdapter = sqlAdapter;
        _parameter = SqlParameterUtil.ConvertToDicParameter(entity) ?? new Dictionary<string, object>();
        _whereClauseList = new List<string>();
    }
    private SqlWhereClauseBuilder(DatabaseType databaseType, TEntity entity = default)
    {
        _sqlAdapter = new DefaultSqlAdapter<TEntity>(databaseType);
        _parameter = SqlParameterUtil.ConvertToDicParameter(entity) ?? new Dictionary<string, object>();
        _whereClauseList = new List<string>();
    }

    public static SqlWhereClauseBuilder<TEntity> Create(ISqlAdapter sqlAdapter, TEntity entity = default)
    {
        return new SqlWhereClauseBuilder<TEntity>(sqlAdapter, entity);
    }
    public static SqlWhereClauseBuilder<TEntity> Create(DatabaseType databaseType, TEntity entity = default)
    {
        return new SqlWhereClauseBuilder<TEntity>(databaseType, entity);
    }

    public virtual SqlWhereClauseBuilder<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
    {
        var whereClause = whereExpression.GetParameterizedWhereClause(_sqlAdapter, _parameter);
        if (!string.IsNullOrEmpty(whereClause))
        {
            _whereClauseList.Add(whereClause);
        }
        return this;
    }

    public virtual SqlWhereClauseBuilder<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
    {
        var aqlAdapter = new DefaultSqlAdapter<TEntity2>(_sqlAdapter.DbType)
        {
            MultiTable = true
        };
        var whereClause = whereExpression.GetParameterizedWhereClause(aqlAdapter, _parameter);
        if (!string.IsNullOrEmpty(whereClause))
        {
            _whereClauseList.Add(whereClause);
        }
        return this;
    }

    public virtual string GetParameterizedWhereClause()
    {
        return string.Join(" AND ", _whereClauseList);
    }
}