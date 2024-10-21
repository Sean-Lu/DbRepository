using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class UpdateableSqlBuilder<TEntity> : BaseSqlBuilder<IUpdateable<TEntity>>, IUpdateable<TEntity>
{
    private const string SqlTemplate = "UPDATE {0} SET {1}{2}";
    private const string SqlTemplateForClickHouse = "ALTER TABLE {0} UPDATE {1}{2}";

    private readonly List<TableFieldInfoForSqlBuilder> _tableFieldList = new();

    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();
    private readonly Lazy<StringBuilder> _where = new();

    private readonly List<Action> _whereActions = new();

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
        var sqlBuilder = new UpdateableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetEntityInfo().TableName);
        if (autoIncludeFields)
        {
            var entityInfo = typeof(TEntity).GetEntityInfo();
            sqlBuilder.UpdateFields(entityInfo.FieldInfos.Select(c => c.FieldName).ToArray());
            sqlBuilder.PrimaryKeyFields(entityInfo.FieldInfos.Where(c => c.IsPrimaryKey).Select(c => c.FieldName).ToArray());
        }
        return sqlBuilder;
    }

    #region [Field]
    public virtual IUpdateable<TEntity> UpdateFields(params string[] fields)
    {
        SqlBuilderUtil.IncludeFields(SqlAdapter, _tableFieldList, fields);
        return this;
    }
    public virtual IUpdateable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _tableFieldList, fields);
        return this;
    }
    public virtual IUpdateable<TEntity> PrimaryKeyFields(params string[] fields)
    {
        SqlBuilderUtil.PrimaryKeyFields(SqlAdapter, _tableFieldList, fields);
        return this;
    }

    public virtual IUpdateable<TEntity> UpdateFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return UpdateFields(fields);
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
        SqlBuilderUtil.IncrementFields(SqlAdapter, _tableFieldList, fieldExpression, value);
        return this;
    }
    public virtual IUpdateable<TEntity> DecrementFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
    {
        SqlBuilderUtil.DecrementFields(SqlAdapter, _tableFieldList, fieldExpression, value);
        return this;
    }
    #endregion

    #region [Join Table]
    public virtual IUpdateable<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IUpdateable<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IUpdateable<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IUpdateable<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual IUpdateable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual IUpdateable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual IUpdateable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual IUpdateable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }

    public virtual IUpdateable<TEntity> InnerJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual IUpdateable<TEntity> LeftJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual IUpdateable<TEntity> RightJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual IUpdateable<TEntity> FullJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    #endregion

    #region [WHERE]
    public virtual IUpdateable<TEntity> Where(string where)
    {
        _whereActions.Add(() => SqlBuilderUtil.Where(_where.Value, where));
        return this;
    }

    public virtual IUpdateable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IUpdateable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
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

    public virtual IUpdateable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }

    public virtual IUpdateable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return Where(condition ? trueWhereExpression : falseWhereExpression);
    }

    public virtual IUpdateable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
    {
        return condition ? Where(whereExpression, aliasName) : this;
    }

    public virtual IUpdateable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity2, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual IUpdateable<TEntity> WhereIF<TEntity2, TEntity3>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity3, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual IUpdateable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        _whereActions.Add(() =>
        {
            SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        });
        return this;
    }
    public virtual IUpdateable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null, string aliasName = null)
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

        var fields = _tableFieldList.Where(c => !c.IsPrimaryKey).ToList();
        if (!fields.Any())
        {
            throw new InvalidOperationException("No fields to update.");
        }

        if (MultiTable)
        {
            SqlAdapter.MultiTable = true;
        }

        var updateFields = fields.Select(fieldInfo =>
        {
            if (fieldInfo.SetFieldCustomHandler != null)
            {
                return fieldInfo.SetFieldCustomHandler(fieldInfo.FieldName, SqlAdapter);
            }

            var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);
            var parameterName = findFieldInfo?.Property.Name ?? fieldInfo.FieldName;
            return $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)}={SqlAdapter.FormatSqlParameter(parameterName)}";
        }).ToList();

        if (_whereActions.Any())
        {
            _whereActions.ForEach(c => c.Invoke());
            _whereActions.Clear();
        }

        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
        {
            // Set the primary key field to the [where] filtering condition.
            if (_tableFieldList.Any(c => c.IsPrimaryKey))
            {
                foreach (var pks in _tableFieldList.Where(c => c.IsPrimaryKey).ToList())
                {
                    var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == pks.FieldName);
                    var parameterName = findFieldInfo?.Property.Name ?? pks.FieldName;
                    SqlBuilderUtil.WhereField<TEntity>(SqlAdapter, _where.Value, entity => pks.FieldName, SqlOperation.Equal, paramName: parameterName);
                }
            }
            else
            {
                foreach (var fieldInfo in tableFieldInfos.Where(c => c.IsPrimaryKey).ToList())
                {
                    var fieldName = fieldInfo.FieldName;
                    var parameterName = fieldInfo.Property?.Name ?? fieldName;
                    SqlBuilderUtil.WhereField<TEntity>(SqlAdapter, _where.Value, entity => fieldName, SqlOperation.Equal, paramName: parameterName);
                }
            }
        }

        if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

        var sb = new StringBuilder();
        sb.Append(string.Format(SqlAdapter.DbType == DatabaseType.ClickHouse ? SqlTemplateForClickHouse : SqlTemplate, $"{SqlAdapter.FormatTableName(TableName)}{JoinTableSql}", string.Join(", ", updateFields), WhereSql));

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}