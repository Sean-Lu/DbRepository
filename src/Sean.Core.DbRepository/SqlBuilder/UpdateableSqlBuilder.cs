using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class UpdateableSqlBuilder<TEntity> : BaseSqlBuilder, IUpdateable<TEntity>
{
    private const string SqlTemplate = "UPDATE {0} SET {1}{2}";
    private const string SqlTemplateForClickHouse = "ALTER TABLE {0} UPDATE {1}{2}";

    private readonly List<TableFieldInfoForSqlBuilder> _includeFieldsList = new();

    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();
    private readonly Lazy<StringBuilder> _where = new();

    private bool MultiTable => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

    private bool _allowEmptyWhereClause;
    private object _parameter;

    private UpdateableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
    {
    }

    /// <summary>
    /// Create an instance of <see cref="IUpdateable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    public static IUpdateable<TEntity> Create(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
    {
        var sqlBuilder = new UpdateableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
        if (autoIncludeFields)
        {
            sqlBuilder.IncludeFields(typeof(TEntity).GetAllFieldNames().ToArray());
            sqlBuilder.PrimaryKeyFields(typeof(TEntity).GetPrimaryKeys().ToArray());
        }
        return sqlBuilder;
    }

    #region [Field]
    public virtual IUpdateable<TEntity> IncludeFields(params string[] fields)
    {
        SqlBuilderUtil.IncludeFields(SqlAdapter, _includeFieldsList, fields);
        return this;
    }
    public virtual IUpdateable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _includeFieldsList, fields);
        return this;
    }
    public virtual IUpdateable<TEntity> PrimaryKeyFields(params string[] fields)
    {
        SqlBuilderUtil.PrimaryKeyFields(SqlAdapter, _includeFieldsList, fields);
        return this;
    }

    public virtual IUpdateable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression, TEntity entity = default)
    {
        if (fieldExpression == null)
        {
            if (entity != null)
            {
                SetParameter(entity);
            }
            return this;
        }

        var fields = fieldExpression.GetFieldNames().ToArray();
        IncludeFields(fields);

        if (entity != null)
        {
            var paramDic = SqlParameterUtil.ConvertToDicParameter(entity);
            if (paramDic != null && paramDic.Any())
            {
                SetParameter(paramDic);
            }
        }

        return this;
    }
    public virtual IUpdateable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IgnoreFields(fields);
    }
    public virtual IUpdateable<TEntity> PrimaryKeyFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return PrimaryKeyFields(fields);
    }

    public virtual IUpdateable<TEntity> IncrementFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
    {
        SqlBuilderUtil.IncrFields(SqlAdapter, _includeFieldsList, fieldExpression, value);
        return this;
    }
    public virtual IUpdateable<TEntity> DecrementFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
    {
        SqlBuilderUtil.DecrFields(SqlAdapter, _includeFieldsList, fieldExpression, value);
        return this;
    }
    #endregion

    #region [Join Table]
    public virtual IUpdateable<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IUpdateable<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IUpdateable<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IUpdateable<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            this._joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual IUpdateable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return InnerJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual IUpdateable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return LeftJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual IUpdateable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return RightJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    public virtual IUpdateable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
    {
        var joinTableName = typeof(TEntity2).GetMainTableName();
        return FullJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
    }
    #endregion

    #region [WHERE]
    public virtual IUpdateable<TEntity> Where(string where)
    {
        SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.None, where);
        return this;
    }
    public virtual IUpdateable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IUpdateable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

    public virtual IUpdateable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }
        SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        return this;
    }
    public virtual IUpdateable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
        {
            MultiTable = true
        };
        SqlBuilderUtil.WhereField(aqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        return this;
    }

    public virtual IUpdateable<TEntity> AndWhere(string where)
    {
        SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.And, where);
        return this;
    }
    public virtual IUpdateable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IUpdateable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

    public virtual IUpdateable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true)
    {
        _allowEmptyWhereClause = allowEmptyWhereClause;
        return this;
    }

    public virtual IUpdateable<TEntity> SetParameter(object param)
    {
        _parameter = param;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
        {
            #region Set the primary key field to the [where] filtering condition.
            if (_includeFieldsList.Any(c => c.PrimaryKey))
            {
                foreach (var pks in _includeFieldsList.Where(c => c.PrimaryKey))
                {
                    var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == pks.FieldName);
                    var parameterName = findFieldInfo?.Property.Name ?? pks.FieldName;
                    WhereField(entity => pks.FieldName, SqlOperation.Equal, paramName: parameterName);
                }
            }
            else
            {
                typeof(TEntity).GetPrimaryKeys().ForEach(fieldName =>
                {
                    var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldName);
                    var parameterName = findFieldInfo?.Property.Name ?? fieldName;
                    WhereField(entity => fieldName, SqlOperation.Equal, paramName: parameterName);
                });
            }
            #endregion
        }

        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

        var fields = _includeFieldsList.Where(c => !c.PrimaryKey).ToList();
        if (!fields.Any())
        {
            throw new InvalidOperationException("No fields to update.");
        }

        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }

        var sets = fields.Select(fieldInfo =>
        {
            if (fieldInfo.SetFieldCustomHandler != null)
            {
                return fieldInfo.SetFieldCustomHandler(fieldInfo.FieldName, SqlAdapter);
            }

            var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);
            var parameterName = findFieldInfo?.Property.Name ?? fieldInfo.FieldName;
            return $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)}={SqlAdapter.FormatInputParameter(parameterName)}";
        });

        var sb = new StringBuilder();
        sb.Append(string.Format(SqlAdapter.DbType == DatabaseType.ClickHouse ? SqlTemplateForClickHouse : SqlTemplate, $"{SqlAdapter.FormatTableName()}{JoinTableSql}", string.Join(", ", sets), WhereSql));

        var sql = new DefaultSqlCommand
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}