using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class QueryableSqlBuilder<TEntity> : BaseSqlBuilder<IQueryable<TEntity>>, IQueryable<TEntity>
{
    //private const string SqlTemplate = "SELECT {1} FROM {0}{2}";

    private readonly List<TableFieldInfoForSqlBuilder> _tableFieldList = new();

    private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
    private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value}" : string.Empty;
    private string GroupBySql => _groupBy.IsValueCreated && _groupBy.Value.Length > 0 ? $" GROUP BY {_groupBy.Value}" : string.Empty;
    private string HavingSql => _having.IsValueCreated && _having.Value.Length > 0 ? $" HAVING {_having.Value}" : string.Empty;
    private string OrderBySql => _orderBy.IsValueCreated && _orderBy.Value.Length > 0 ? $" ORDER BY {_orderBy.Value}" : string.Empty;

    private readonly Lazy<StringBuilder> _joinTable = new();
    private readonly Lazy<StringBuilder> _where = new();
    private readonly Lazy<StringBuilder> _groupBy = new();
    private readonly Lazy<StringBuilder> _having = new();
    private readonly Lazy<StringBuilder> _orderBy = new();

    private readonly List<Action> _whereActions = new();
    private readonly List<Action> _orderByActions = new();

    private bool MultiTable => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

    private object _parameter;

    private int? _topNumber;
    private int? _pageNumber;
    private int? _pageSize;
    private int? _offset;
    private int? _rows;

    private QueryableSqlBuilder(DatabaseType dbType) : base(dbType, typeof(TEntity).GetEntityInfo().TableName)
    {
    }

    /// <summary>
    /// Create an instance of <see cref="IQueryable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
    /// <returns></returns>
    public static IQueryable<TEntity> Create(DatabaseType dbType, bool autoIncludeFields)
    {
        var sqlBuilder = new QueryableSqlBuilder<TEntity>(dbType);
        if (autoIncludeFields)
        {
            sqlBuilder.SelectFields(typeof(TEntity).GetEntityInfo().FieldInfos.Select(c => c.FieldName).ToArray());
        }
        return sqlBuilder;
    }

    #region [Field]
    public virtual IQueryable<TEntity> SelectFields(params string[] fields)
    {
        SqlBuilderUtil.IncludeFields(SqlAdapter, _tableFieldList, fields);
        return this;
    }
    public virtual IQueryable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _tableFieldList, fields);
        return this;
    }

    public virtual IQueryable<TEntity> MaxField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        SqlBuilderUtil.MaxField(SqlAdapter, _tableFieldList, fieldName, aliasName, fieldNameFormatted);
        return this;
    }
    public virtual IQueryable<TEntity> MinField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        SqlBuilderUtil.MinField(SqlAdapter, _tableFieldList, fieldName, aliasName, fieldNameFormatted);
        return this;
    }
    public virtual IQueryable<TEntity> SumField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        SqlBuilderUtil.SumField(SqlAdapter, _tableFieldList, fieldName, aliasName, fieldNameFormatted);
        return this;
    }
    public virtual IQueryable<TEntity> AvgField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        SqlBuilderUtil.AvgField(SqlAdapter, _tableFieldList, fieldName, aliasName, fieldNameFormatted);
        return this;
    }
    public virtual IQueryable<TEntity> CountField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        SqlBuilderUtil.CountField(SqlAdapter, _tableFieldList, fieldName, aliasName, fieldNameFormatted);
        return this;
    }
    public virtual IQueryable<TEntity> CountDistinctField(string fieldName, string aliasName = null)
    {
        SqlBuilderUtil.CountDistinctField(SqlAdapter, _tableFieldList, fieldName, aliasName);
        return this;
    }
    public virtual IQueryable<TEntity> DistinctFields(params string[] fields)
    {
        SqlBuilderUtil.DistinctFields<TEntity>(SqlAdapter, _tableFieldList, fields);
        return this;
    }

    public virtual IQueryable<TEntity> SelectFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return SelectFields(fields);
    }
    public virtual IQueryable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IgnoreFields(fields);
    }

    public virtual IQueryable<TEntity> MaxField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames();
        fields.ForEach(fieldName => MaxField(fieldName, aliasName, fieldNameFormatted));
        return this;
    }
    public virtual IQueryable<TEntity> MinField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames();
        fields.ForEach(fieldName => MinField(fieldName, aliasName, fieldNameFormatted));
        return this;
    }
    public virtual IQueryable<TEntity> SumField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames();
        fields.ForEach(fieldName => SumField(fieldName, aliasName, fieldNameFormatted));
        return this;
    }
    public virtual IQueryable<TEntity> AvgField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames();
        fields.ForEach(fieldName => AvgField(fieldName, aliasName, fieldNameFormatted));
        return this;
    }
    public virtual IQueryable<TEntity> CountField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames();
        fields.ForEach(fieldName => CountField(fieldName, aliasName, fieldNameFormatted));
        return this;
    }
    public virtual IQueryable<TEntity> CountDistinctField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames();
        fields.ForEach(fieldName => CountDistinctField(fieldName, aliasName));
        return this;
    }
    public virtual IQueryable<TEntity> DistinctFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return DistinctFields(fields);
    }
    #endregion

    #region [Join Table]
    public virtual IQueryable<TEntity> InnerJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" INNER JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IQueryable<TEntity> LeftJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IQueryable<TEntity> RightJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
        }
        return this;
    }
    public virtual IQueryable<TEntity> FullJoin(string joinTableSql)
    {
        if (!string.IsNullOrWhiteSpace(joinTableSql))
        {
            _joinTable.Value.Append($" FULL JOIN {joinTableSql}");
        }
        return this;
    }

    public virtual IQueryable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual IQueryable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual IQueryable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }
    public virtual IQueryable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, SqlAdapter.AliasName, rightTableAliasName));
    }

    public virtual IQueryable<TEntity> InnerJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return InnerJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual IQueryable<TEntity> LeftJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return LeftJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual IQueryable<TEntity> RightJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return RightJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    public virtual IQueryable<TEntity> FullJoin<TEntity2, TEntity3>(Expression<Func<TEntity2, object>> leftTableFieldExpression, Expression<Func<TEntity3, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        return FullJoin(SqlBuilderUtil.GetJoinSql(SqlAdapter, leftTableFieldExpression, rightTableFieldExpression, leftTableAliasName, rightTableAliasName));
    }
    #endregion

    #region [WHERE]
    public virtual IQueryable<TEntity> Where(string where)
    {
        _whereActions.Add(() => SqlBuilderUtil.Where(_where.Value, where));
        return this;
    }

    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
    public virtual IQueryable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
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

    public virtual IQueryable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> whereExpression)
    {
        return condition ? Where(whereExpression) : this;
    }

    public virtual IQueryable<TEntity> WhereIF(bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return Where(condition ? trueWhereExpression : falseWhereExpression);
    }

    public virtual IQueryable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> whereExpression, string aliasName = null)
    {
        return condition ? Where(whereExpression, aliasName) : this;
    }

    public virtual IQueryable<TEntity> WhereIF<TEntity2>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity2, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual IQueryable<TEntity> WhereIF<TEntity2, TEntity3>(bool condition, Expression<Func<TEntity2, bool>> trueWhereExpression, Expression<Func<TEntity3, bool>> falseWhereExpression, string trueAliasName = null, string falseAliasName = null)
    {
        return condition ? Where(trueWhereExpression, trueAliasName) : Where(falseWhereExpression, falseAliasName);
    }

    public virtual IQueryable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
    {
        _whereActions.Add(() =>
        {
            SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
        });
        return this;
    }
    public virtual IQueryable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null, string aliasName = null)
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

    #region [GROUP BY]
    /// <summary>
    /// GROUP BY column_name
    /// </summary>
    /// <param name="groupBy">The [GROUP BY] keyword is not included.</param>
    /// <returns></returns>
    public virtual IQueryable<TEntity> GroupBy(string groupBy)
    {
        if (!string.IsNullOrWhiteSpace(groupBy))
        {
            if (_groupBy.Value.Length > 0) _groupBy.Value.Append(" ");
            _groupBy.Value.Append(groupBy);
        }
        return this;
    }
    public virtual IQueryable<TEntity> GroupBy(Expression<Func<TEntity, object>> fieldExpression)
    {
        var fieldNames = fieldExpression.GetFieldNames();
        if (fieldNames != null && fieldNames.Any())
        {
            if (MultiTable)
            {
                SqlAdapter.MultiTable = true;
            }
            GroupBy(string.Join(", ", fieldNames.Select(fieldName => SqlAdapter.FormatFieldName(fieldName)).ToList()));
        }
        return this;
    }
    #endregion

    #region [HAVING]
    /// <summary>
    /// HAVING aggregate_function(column_name) operator value
    /// </summary>
    /// <param name="having">The [HAVING] keyword is not included.</param>
    /// <returns></returns>
    public virtual IQueryable<TEntity> Having(string having)
    {
        if (!string.IsNullOrWhiteSpace(having))
        {
            if (_having.Value.Length > 0) _having.Value.Append(" ");
            _having.Value.Append(having);
        }
        return this;
    }
    #endregion

    #region [ORDER BY]
    /// <summary>
    /// ORDER BY column_name,column_name ASC|DESC;
    /// </summary>
    /// <param name="orderBy">The [ORDER BY] keyword is not included.</param>
    /// <returns></returns>
    public virtual IQueryable<TEntity> OrderBy(string orderBy)
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
    public virtual IQueryable<TEntity> OrderBy(OrderByCondition orderBy)
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
    public virtual IQueryable<TEntity> OrderBy(OrderByType type, params string[] fieldNames)
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
    public virtual IQueryable<TEntity> OrderBy(OrderByType type, Expression<Func<TEntity, object>> fieldExpression)
    {
        return OrderBy(type, fieldExpression.GetFieldNames().ToArray());
    }
    public virtual IQueryable<TEntity> OrderBy<TEntity2>(OrderByType type, Expression<Func<TEntity2, object>> fieldExpression, string aliasName = null)
    {
        _orderByActions.Add(() =>
        {
            var fieldNames = fieldExpression.GetFieldNames();
            if (fieldNames != null && fieldNames.Any())
            {
                if (_orderBy.Value.Length > 0) _orderBy.Value.Append(", ");

                _orderBy.Value.Append($"{string.Join(", ", fieldNames.Select(fieldName => SqlAdapter.FormatFieldName(fieldName, typeof(TEntity2).GetEntityInfo().TableName, aliasName)).ToList())} {type.ToSqlString()}");
            }
        });
        return this;
    }
    #endregion

    public virtual IQueryable<TEntity> Top(int? top)
    {
        _topNumber = top;
        return this;
    }
    public virtual IQueryable<TEntity> Page(int? pageNumber, int? pageSize)
    {
        _pageNumber = pageNumber;
        _pageSize = pageSize;
        return this;
    }
    public virtual IQueryable<TEntity> Offset(int? offset, int? rows)
    {
        _offset = offset;
        _rows = rows;
        return this;
    }

    public virtual IQueryable<TEntity> SetParameter(object param)
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

        var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
        var selectFields = _tableFieldList.Any() ? string.Join(", ", _tableFieldList.Select(fieldInfo =>
        {
            if (fieldInfo.IsFieldNameFormatted)
            {
                return !string.IsNullOrWhiteSpace(fieldInfo.AliasName) ? $"{fieldInfo.FieldName} AS {fieldInfo.AliasName}" : $"{fieldInfo.FieldName}";
            }

            if (!string.IsNullOrWhiteSpace(fieldInfo.AliasName))
            {
                return $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)} AS {fieldInfo.AliasName}";
            }

            var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);
            return findFieldInfo != null && findFieldInfo.Property.Name != fieldInfo.FieldName
                ? $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)} AS {findFieldInfo.Property.Name}"
                : $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)}";
        }).ToList()) : "*";

        if (_whereActions.Any())
        {
            _whereActions.ForEach(c => c.Invoke());
            _whereActions.Clear();
        }
        if (_orderByActions.Any())
        {
            _orderByActions.ForEach(c => c.Invoke());
            _orderByActions.Clear();
        }

        var sql = new DefaultSqlCommand(SqlAdapter.DbType);
        if (_topNumber.HasValue)
        {
            // 查询前几行
            switch (SqlAdapter.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.MariaDB:
                case DatabaseType.TiDB:
                case DatabaseType.OceanBase:
                case DatabaseType.SQLite:
                case DatabaseType.DuckDB:
                case DatabaseType.PostgreSql:
                case DatabaseType.OpenGauss:
                case DatabaseType.HighgoDB:
                case DatabaseType.IvorySQL:
                case DatabaseType.QuestDB:
                case DatabaseType.ClickHouse:
                case DatabaseType.KingbaseES:
                case DatabaseType.ShenTong:
                case DatabaseType.Xugu:
                    sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber}";
                    break;
                case DatabaseType.SqlServer:
                case DatabaseType.MsAccess:
                case DatabaseType.Dameng:
                    sql.Sql = $"SELECT TOP {_topNumber} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}";
                    break;
                case DatabaseType.Firebird:
                case DatabaseType.Informix:
                    sql.Sql = $"SELECT FIRST {_topNumber} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}";
                    break;
                case DatabaseType.Oracle:
                    var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {_topNumber}" : $"{WhereSql} AND ROWNUM <= {_topNumber}";
                    sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}{OrderBySql}";
                    break;
                default:
                    sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber}";
                    break;
            }
        }
        else if (_pageNumber.HasValue && _pageSize.HasValue)
        {
            // 分页查询
            var offset = (_pageNumber.Value - 1) * _pageSize.Value;// 偏移量
            var rows = _pageSize.Value;// 行数
            sql.Sql = GetQuerySql(selectFields, offset, rows);
        }
        else if (_offset.HasValue && _rows.HasValue)
        {
            // 根据偏移量查询
            sql.Sql = GetQuerySql(selectFields, _offset.Value, _rows.Value);
        }
        else
        {
            // 普通查询
            sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}";
        }

        sql.Parameter = _parameter;
        return sql;
    }

    private string GetQuerySql(string selectFields, int offset, int rows)
    {
        switch (SqlAdapter.DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.OceanBase:
            case DatabaseType.SQLite:
            case DatabaseType.ClickHouse:
                return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows}";
            case DatabaseType.PostgreSql:
            case DatabaseType.OpenGauss:
            case DatabaseType.HighgoDB:
            case DatabaseType.IvorySQL:
            case DatabaseType.KingbaseES:
            case DatabaseType.ShenTong:
            case DatabaseType.Xugu:
            case DatabaseType.DuckDB:
                return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {rows} OFFSET {offset}";
            case DatabaseType.QuestDB:
                return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows + offset}";
            case DatabaseType.SqlServer:
                {
                    if (DbContextConfiguration.SqlServerOptions is { UseRowNumberForPaging: true })
                    {
                        return GetRowNumberQuerySql(selectFields, offset, rows);// SQL Server 2005 ~ 2008
                    }

                    return GetOffsetQuerySql(selectFields, offset, rows);// SQL Server 2012 ~ +
                }
            case DatabaseType.Dameng:
                return GetOffsetQuerySql(selectFields, offset, rows, false);
            case DatabaseType.DB2:
                return GetRowNumberQuerySql(selectFields, offset, rows);
            case DatabaseType.MsAccess:
                {
                    if (offset == 0)
                    {
                        return $"SELECT TOP {rows} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}";
                    }

                    var keyFieldName = typeof(TEntity).GetEntityInfo().FieldInfos.Where(c => c.IsPrimaryKey).Select(c => c.FieldName).FirstOrDefault();
                    var keyFilterSql = $"SELECT TOP {offset} {SqlAdapter.FormatFieldName(keyFieldName)} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}";
                    var sqlWhere = $"{(!string.IsNullOrEmpty(WhereSql) ? $"{WhereSql} AND" : " WHERE")} {SqlAdapter.FormatFieldName(keyFieldName)} NOT IN ({keyFilterSql})";
                    return $"SELECT TOP {rows} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}{OrderBySql}";
                }
            case DatabaseType.Firebird:
                return $"SELECT FIRST {rows} SKIP {offset} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}";
            case DatabaseType.Informix:
                return $"SELECT SKIP {offset} FIRST {rows} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}";
            case DatabaseType.Oracle:
                {
                    if (!string.IsNullOrWhiteSpace(OrderBySql))
                    {
                        return GetRowNumberQuerySql(selectFields, offset, rows);
                    }

                    var sqlWhere = $"{(!string.IsNullOrEmpty(WhereSql) ? $"{WhereSql} AND" : " WHERE")} ROWNUM <= {offset + rows}";
                    return $"SELECT {selectFields} FROM (SELECT ROWNUM ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset}";
                }
            default:
                return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows}";
        }
    }

    private string GetRowNumberQuerySql(string selectFields, int offset, int rows)
    {
        var orderBy = OrderBySql;
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            orderBy = " ORDER BY (SELECT 1)";
        }

        return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({orderBy}) ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset} AND t2.ROW_NUM <= {offset + rows}";
    }
    private string GetOffsetQuerySql(string selectFields, int offset, int rows, bool setDefaultOrderByIfNull = true)
    {
        var orderBy = OrderBySql;
        if (setDefaultOrderByIfNull && string.IsNullOrWhiteSpace(orderBy))
        {
            orderBy = " ORDER BY (SELECT 1)";
        }

        return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{orderBy} OFFSET {offset} ROWS FETCH NEXT {rows} ROWS ONLY";
    }
}