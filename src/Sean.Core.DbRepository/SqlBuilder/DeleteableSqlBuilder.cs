﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class DeleteableSqlBuilder<TEntity> : BaseSqlBuilder, IDeleteable<TEntity>
{
    private const string SqlTemplate = "DELETE FROM {0}{1}";

    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();
    private readonly Lazy<StringBuilder> _where = new();

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
        var sqlBuilder = new DeleteableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
        return sqlBuilder;
    }

    #region [Join Table]
    public virtual IDeleteable<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IDeleteable<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IDeleteable<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IDeleteable<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual IDeleteable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return InnerJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual IDeleteable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return LeftJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual IDeleteable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return RightJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual IDeleteable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return FullJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    #endregion

    #region [WHERE]
    public virtual IDeleteable<TEntity> Where(string where)
    {
        SqlBuilderUtil.Where(_where.Value, where);
        return this;
    }

    public virtual IDeleteable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IDeleteable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

    public IDeleteable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }
    public IDeleteable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }

    public virtual IDeleteable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }
        SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        return this;
    }
    public virtual IDeleteable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
        {
            MultiTable = true
        };
        SqlBuilderUtil.WhereField(aqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
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
        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
        {
            #region Set the primary key field to the [where] filtering condition.
            var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            typeof(TEntity).GetPrimaryKeys().ForEach(fieldName =>
            {
                var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldName);
                var parameterName = findFieldInfo?.Property.Name ?? fieldName;
                WhereField(entity => fieldName, SqlOperation.Equal, paramName: parameterName);
            });
            #endregion
        }

        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

        var sb = new StringBuilder();
        sb.Append(string.Format(SqlTemplate, $"{SqlAdapter.FormatTableName()}{JoinTableSql}", WhereSql));

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}