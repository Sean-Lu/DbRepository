using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public abstract class DeleteableSqlBuilder : BaseSqlBuilder
{
    public const string SqlTemplate = "DELETE FROM {0}{1}";

    protected DeleteableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
    {
    }
}

public class DeleteableSqlBuilder<TEntity> : DeleteableSqlBuilder, IDeleteable<TEntity>
{
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

    #region [Join] 表关联
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
        SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.None, where);
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

    public virtual IDeleteable<TEntity> AndWhere(string where)
    {
        SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.And, where);
        return this;
    }
    public virtual IDeleteable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IDeleteable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

    public virtual ISqlCommand Build()
    {
        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
        {
            #region 设置默认过滤条件为主键字段
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

        var sql = new DefaultSqlCommand
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}

public interface IDeleteable
{
    ISqlAdapter SqlAdapter { get; }

    /// <summary>
    /// 创建删除数据的SQL：<see cref="DeleteableSqlBuilder.SqlTemplate"/>
    /// <para>1. 为了防止误删除，需要指定WHERE过滤条件，否则会抛出异常，可以通过 <see cref="IDeleteable{TEntity}.AllowEmptyWhereClause"/> 设置允许空 WHERE 子句</para>
    /// <para>2. 如果没有指定WHERE过滤条件，且没有设置 <see cref="IDeleteable{TEntity}.AllowEmptyWhereClause"/> 为true，则过滤条件默认使用 <see cref="KeyAttribute"/> 主键字段</para>
    /// </summary>
    /// <returns></returns>
    ISqlCommand Build();
}

public interface IDeleteable<TEntity> : IDeleteable
{
    #region [Join] 表关联
    /// <summary>
    /// INNER JOIN
    /// </summary>
    /// <param name="joinTableSql">不包含关键字：INNER JOIN</param>
    /// <returns></returns>
    IDeleteable<TEntity> InnerJoin(string joinTableSql);
    /// <summary>
    /// LEFT JOIN
    /// </summary>
    /// <param name="joinTableSql">不包含关键字：LEFT JOIN</param>
    /// <returns></returns>
    IDeleteable<TEntity> LeftJoin(string joinTableSql);
    /// <summary>
    /// RIGHT JOIN
    /// </summary>
    /// <param name="joinTableSql">不包含关键字：RIGHT JOIN</param>
    /// <returns></returns>
    IDeleteable<TEntity> RightJoin(string joinTableSql);
    /// <summary>
    /// FULL JOIN
    /// </summary>
    /// <param name="joinTableSql">不包含关键字：FULL JOIN</param>
    /// <returns></returns>
    IDeleteable<TEntity> FullJoin(string joinTableSql);

    /// <summary>
    /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    /// <summary>
    /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <param name="fieldExpression2"></param>
    /// <returns></returns>
    IDeleteable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
    #endregion

    #region [WHERE]
    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="where">不包含关键字：WHERE</param>
    /// <returns></returns>
    IDeleteable<TEntity> Where(string where);
    /// <summary>
    /// 解析WHERE过滤条件
    /// </summary>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <returns></returns>
    IDeleteable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
    IDeleteable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

    /// <summary>
    /// WHERE column_name operator value
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    /// <param name="operation"></param>
    /// <param name="keyword"></param>
    /// <param name="include"></param>
    /// <param name="paramName">参数名称，默认同 <paramref name="fieldName"/></param>
    /// <returns></returns>
    IDeleteable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
    IDeleteable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

    IDeleteable<TEntity> AndWhere(string where);
    IDeleteable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
    IDeleteable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
    #endregion

    /// <summary>
    /// 是否允许空的 WHERE 子句
    /// <para>注：为了防止执行错误的SQL导致不可逆的结果，默认不允许空的WHERE子句</para>
    /// </summary>
    /// <param name="allowEmptyWhereClause"></param>
    /// <returns></returns>
    IDeleteable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true);

    /// <summary>
    /// 设置SQL入参
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    IDeleteable<TEntity> SetParameter(object param);
}