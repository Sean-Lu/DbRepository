using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository
{
    public abstract class QueryableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "SELECT {1} FROM {0}{2};";

        protected QueryableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
        }
    }

    public class QueryableSqlBuilder<TEntity> : QueryableSqlBuilder, IQueryable<TEntity>
    {
        private readonly List<TableFieldInfoForSqlBuilder> _includeFieldsList = new();

        private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
        private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;
        private string GroupBySql => _groupBy.IsValueCreated && _groupBy.Value.Length > 0 ? $" GROUP BY {_groupBy.Value.ToString()}" : string.Empty;
        private string HavingSql => _having.IsValueCreated && _having.Value.Length > 0 ? $" HAVING {_having.Value.ToString()}" : string.Empty;
        private string OrderBySql => _orderBy.IsValueCreated && _orderBy.Value.Length > 0 ? $" ORDER BY {_orderBy.Value.ToString()}" : string.Empty;

        private readonly Lazy<StringBuilder> _joinTable = new();
        private readonly Lazy<StringBuilder> _where = new();
        private readonly Lazy<StringBuilder> _groupBy = new();
        private readonly Lazy<StringBuilder> _having = new();
        private readonly Lazy<StringBuilder> _orderBy = new();

        private bool MultiTable => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

        private object _parameter;

        private int? _topNumber;
        private int? _pageIndex;
        private int? _pageSize;
        private int? _offset;
        private int? _rows;

        private QueryableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
        }

        /// <summary>
        /// Create an instance of <see cref="IQueryable{TEntity}"/>.
        /// </summary>
        /// <param name="dbType">Database type.</param>
        /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static IQueryable<TEntity> Create(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            var sqlBuilder = new QueryableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
            if (autoIncludeFields)
            {
                sqlBuilder.IncludeFields(typeof(TEntity).GetAllFieldNames().ToArray());
            }
            return sqlBuilder;
        }

        #region [Field]
        public virtual IQueryable<TEntity> IncludeFields(params string[] fields)
        {
            SqlBuilderUtil.IncludeFields(SqlAdapter, _includeFieldsList, fields);
            return this;
        }
        public virtual IQueryable<TEntity> IgnoreFields(params string[] fields)
        {
            SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _includeFieldsList, fields);
            return this;
        }

        public IQueryable<TEntity> MaxField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
        {
            SqlBuilderUtil.MaxField(SqlAdapter, _includeFieldsList, fieldName, aliasName, fieldNameFormatted);
            return this;
        }
        public IQueryable<TEntity> MinField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
        {
            SqlBuilderUtil.MinField(SqlAdapter, _includeFieldsList, fieldName, aliasName, fieldNameFormatted);
            return this;
        }
        public IQueryable<TEntity> SumField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
        {
            SqlBuilderUtil.SumField(SqlAdapter, _includeFieldsList, fieldName, aliasName, fieldNameFormatted);
            return this;
        }
        public IQueryable<TEntity> AvgField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
        {
            SqlBuilderUtil.AvgField(SqlAdapter, _includeFieldsList, fieldName, aliasName, fieldNameFormatted);
            return this;
        }
        public IQueryable<TEntity> CountField(string fieldName, string aliasName = null, bool fieldNameFormatted = false)
        {
            SqlBuilderUtil.CountField(SqlAdapter, _includeFieldsList, fieldName, aliasName, fieldNameFormatted);
            return this;
        }
        public IQueryable<TEntity> CountDistinctField(string fieldName, string aliasName = null)
        {
            SqlBuilderUtil.CountDistinctField(SqlAdapter, _includeFieldsList, fieldName, aliasName);
            return this;
        }
        public IQueryable<TEntity> DistinctFields(params string[] fields)
        {
            SqlBuilderUtil.DistinctFields<TEntity>(SqlAdapter, _includeFieldsList, fields);
            return this;
        }

        public virtual IQueryable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames().ToArray();
            return IncludeFields(fields);
        }
        public virtual IQueryable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames().ToArray();
            return IgnoreFields(fields);
        }

        public IQueryable<TEntity> MaxField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames();
            fields.ForEach(fieldName => MaxField(fieldName, aliasName, fieldNameFormatted));
            return this;
        }
        public IQueryable<TEntity> MinField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames();
            fields.ForEach(fieldName => MinField(fieldName, aliasName, fieldNameFormatted));
            return this;
        }
        public IQueryable<TEntity> SumField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames();
            fields.ForEach(fieldName => SumField(fieldName, aliasName, fieldNameFormatted));
            return this;
        }
        public IQueryable<TEntity> AvgField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames();
            fields.ForEach(fieldName => AvgField(fieldName, aliasName, fieldNameFormatted));
            return this;
        }
        public IQueryable<TEntity> CountField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames();
            fields.ForEach(fieldName => CountField(fieldName, aliasName, fieldNameFormatted));
            return this;
        }
        public IQueryable<TEntity> CountDistinctField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames();
            fields.ForEach(fieldName => CountDistinctField(fieldName, aliasName));
            return this;
        }
        public IQueryable<TEntity> DistinctFields(Expression<Func<TEntity, object>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetFieldNames().ToArray();
            return DistinctFields(fields);
        }
        #endregion

        #region [Join] 表关联
        public virtual IQueryable<TEntity> InnerJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" INNER JOIN {joinTableSql}");
            }
            return this;
        }
        public virtual IQueryable<TEntity> LeftJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
            }
            return this;
        }
        public virtual IQueryable<TEntity> RightJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
            }
            return this;
        }
        public virtual IQueryable<TEntity> FullJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" FULL JOIN {joinTableSql}");
            }
            return this;
        }

        public virtual IQueryable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return InnerJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        public virtual IQueryable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return LeftJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        public virtual IQueryable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return RightJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        public virtual IQueryable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return FullJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        #endregion

        #region [WHERE]
        public virtual IQueryable<TEntity> Where(string where)
        {
            SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.None, where);
            return this;
        }
        public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
        public virtual IQueryable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

        public virtual IQueryable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            if (MultiTable)
            {
                SqlAdapter.MultiTable = true;
            }
            SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
            return this;
        }
        public virtual IQueryable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            var aqlAdapter = new DefaultSqlAdapter<TEntity2>(SqlAdapter.DbType)
            {
                MultiTable = true
            };
            SqlBuilderUtil.WhereField(aqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
            return this;
        }

        public virtual IQueryable<TEntity> AndWhere(string where)
        {
            SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.And, where);
            return this;
        }
        public virtual IQueryable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression)
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
        public virtual IQueryable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

        #region [GROUP BY]
        /// <summary>
        /// GROUP BY column_name
        /// </summary>
        /// <param name="groupBy">不包含关键字：GROUP BY</param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> GroupBy(string groupBy)
        {
            if (!string.IsNullOrWhiteSpace(groupBy))
            {
                if (_groupBy.Value.Length > 0) this._groupBy.Value.Append(" ");
                this._groupBy.Value.Append(groupBy);
            }
            return this;
        }
        public virtual IQueryable<TEntity> GroupByField(params string[] fieldNames)
        {
            if (fieldNames != null && fieldNames.Any())
            {
                if (MultiTable)
                {
                    SqlAdapter.MultiTable = true;
                }
                GroupBy(string.Join(", ", fieldNames.Select(fieldName => SqlAdapter.FormatFieldName(fieldName))));
            }
            return this;
        }
        public virtual IQueryable<TEntity> GroupByField(Expression<Func<TEntity, object>> fieldExpression)
        {
            return GroupByField(fieldExpression.GetFieldNames().ToArray());
        }
        #endregion

        #region [HAVING]
        /// <summary>
        /// HAVING aggregate_function(column_name) operator value
        /// </summary>
        /// <param name="having">不包含关键字：HAVING</param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> Having(string having)
        {
            if (!string.IsNullOrWhiteSpace(having))
            {
                if (_having.Value.Length > 0) this._having.Value.Append(" ");
                this._having.Value.Append(having);
            }
            return this;
        }
        #endregion

        #region [ORDER BY]
        /// <summary>
        /// ORDER BY column_name,column_name ASC|DESC;
        /// </summary>
        /// <param name="orderBy">不包含关键字：ORDER BY</param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> OrderBy(string orderBy)
        {
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                if (_orderBy.Value.Length > 0) this._orderBy.Value.Append(", ");
                this._orderBy.Value.Append(orderBy);
            }
            return this;
        }
        public virtual IQueryable<TEntity> OrderBy(OrderByCondition orderBy)
        {
            orderBy?.Resolve((type, fields, orderByValue) =>
            {
                if (!string.IsNullOrEmpty(orderByValue))
                {
                    OrderBy(orderByValue);
                }
                else
                {
                    OrderByField(type, fields);
                }
            });
            return this;
        }
        public virtual IQueryable<TEntity> OrderByField(OrderByType type, params string[] fieldNames)
        {
            if (fieldNames != null && fieldNames.Any())
            {
                if (_orderBy.Value.Length > 0) this._orderBy.Value.Append(", ");

                if (MultiTable)
                {
                    SqlAdapter.MultiTable = true;
                }
                _orderBy.Value.Append($"{string.Join(", ", fieldNames.Select(fieldName => SqlAdapter.FormatFieldName(fieldName)))} {type.ToSqlString()}");
            }
            return this;
        }
        public virtual IQueryable<TEntity> OrderByField(OrderByType type, Expression<Func<TEntity, object>> fieldExpression)
        {
            return OrderByField(type, fieldExpression.GetFieldNames().ToArray());
        }
        #endregion

        public virtual IQueryable<TEntity> Top(int? top)
        {
            this._topNumber = top;
            return this;
        }
        public virtual IQueryable<TEntity> Page(int? pageIndex, int? pageSize)
        {
            this._pageIndex = pageIndex;
            this._pageSize = pageSize;
            return this;
        }
        public virtual IQueryable<TEntity> Offset(int? offset, int? rows)
        {
            this._offset = offset;
            this._rows = rows;
            return this;
        }

        public virtual IQueryable<TEntity> SetParameter(object param)
        {
            _parameter = param;
            return this;
        }

        public virtual ISqlCommand Build()
        {
            if (MultiTable)
            {
                SqlAdapter.MultiTable = true;
            }

            var sql = new DefaultSqlCommand();
            var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            var selectFields = _includeFieldsList.Any() ? string.Join(", ", _includeFieldsList.Select(fieldInfo =>
            {
                if (!fieldInfo.FieldNameFormatted)
                {
                    if (!string.IsNullOrWhiteSpace(fieldInfo.AliasName))
                    {
                        return $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)} AS {fieldInfo.AliasName}";
                    }

                    var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);
                    if (findFieldInfo != null && findFieldInfo.Property.Name != fieldInfo.FieldName)
                    {
                        return $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)} AS {findFieldInfo.Property.Name}"; // SELECT column_name AS alias_name
                    }

                    return $"{SqlAdapter.FormatFieldName(fieldInfo.FieldName)}";
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(fieldInfo.AliasName))
                    {
                        return $"{fieldInfo.FieldName} AS {fieldInfo.AliasName}";
                    }

                    return $"{fieldInfo.FieldName}";
                }
            })) : "*";
            //const string rowNumAlias = "ROW_NUM";
            if (_topNumber.HasValue)
            {
                // 查询前几行
                switch (SqlAdapter.DbType)
                {
                    case DatabaseType.MySql:
                    case DatabaseType.SQLite:
                    case DatabaseType.PostgreSql:
                        sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber};";
                        break;
                    case DatabaseType.SqlServer:
                    case DatabaseType.Access:
                        sql.Sql = $"SELECT TOP {_topNumber} {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                        break;
                    case DatabaseType.Oracle:
                        var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {_topNumber}" : $"{WhereSql} AND ROWNUM <= {_topNumber}";
                        sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}{OrderBySql};";
                        break;
                    default:
                        sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber};";
                        break;
                }
            }
            else if (_pageIndex.HasValue && _pageSize.HasValue)
            {
                // 分页查询
                var offset = (_pageIndex.Value - 1) * _pageSize.Value;// 偏移量
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
                sql.Sql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
            }

            sql.Parameter = _parameter;
            return sql;
        }

        private string GetQuerySql(string selectFields, int offset, int rows)
        {
            switch (SqlAdapter.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows};";
                case DatabaseType.PostgreSql:
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {rows} OFFSET {offset};";
                case DatabaseType.SqlServer:
                    if (DbContextConfiguration.SqlServerOptions is { UseRowNumberForPaging: true })
                    {
                        return GetRowNumberQuerySql(selectFields, offset, rows);// SQL Server 2005 ~ 2008
                    }

                    return GetOffsetQuerySql(selectFields, offset, rows);// SQL Server 2012 ~ +
                case DatabaseType.DB2:
                    return GetRowNumberQuerySql(selectFields, offset, rows);
                case DatabaseType.Access:
                    return $"SELECT TOP {rows} {selectFields} FROM (SELECT TOP {offset + rows} {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}) t2;";
                case DatabaseType.Oracle:
                    if (!string.IsNullOrWhiteSpace(OrderBySql))
                    {
                        return GetRowNumberQuerySql(selectFields, offset, rows);
                    }

                    var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {offset + rows}" : $"{WhereSql} AND ROWNUM <= {offset + rows}";
                    return $"SELECT {selectFields} FROM (SELECT ROWNUM ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset};";
                default:
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows};";
            }
        }

        private string GetRowNumberQuerySql(string selectFields, int offset, int rows)
        {
            var orderBy = OrderBySql;
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                orderBy = " ORDER BY (SELECT 1)";
            }

            return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({orderBy}) ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset} AND t2.ROW_NUM <= {offset + rows};";
        }
        private string GetOffsetQuerySql(string selectFields, int offset, int rows)
        {
            var orderBy = OrderBySql;
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                orderBy = " ORDER BY (SELECT 1)";
            }

            return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{orderBy} OFFSET {offset} ROWS FETCH NEXT {rows} ROWS ONLY;";
        }
    }

    public interface IQueryable
    {
        ISqlAdapter SqlAdapter { get; }

        /// <summary>
        /// 创建查询数据的SQL：<see cref="QueryableSqlBuilder.SqlTemplate"/>
        /// </summary>
        /// <returns></returns>
        ISqlCommand Build();
    }

    public interface IQueryable<TEntity> : IQueryable
    {
        #region [Field]
        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fields">字段名称</param>
        /// <returns></returns>
        IQueryable<TEntity> IncludeFields(params string[] fields);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fields">字段名称</param>
        /// <returns></returns>
        IQueryable<TEntity> IgnoreFields(params string[] fields);

        /// <summary>
        /// MAX() - 返回最大值
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted"><paramref name="fieldName"/> 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> MaxField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// MIN() - 返回最小值
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted"><paramref name="fieldName"/> 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> MinField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// SUM() - 返回总和
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted"><paramref name="fieldName"/> 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> SumField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// AVG() - 返回平均值
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted"><paramref name="fieldName"/> 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> AvgField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// COUNT() - 返回行数
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted"><paramref name="fieldName"/> 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> CountField(string fieldName, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// SELECT COUNT(DISTINCT field_name) FROM table_name;
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="aliasName">别名</param>
        /// <returns></returns>
        IQueryable<TEntity> CountDistinctField(string fieldName, string aliasName = null);
        /// <summary>
        /// SELECT DISTINCT field_name1,field_name2 FROM table_name;
        /// </summary>
        /// <param name="fields">字段名称</param>
        /// <returns></returns>
        IQueryable<TEntity> DistinctFields(params string[] fields);

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IQueryable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IQueryable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression);

        /// <summary>
        /// MAX() - 返回最大值
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted">fieldName 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> MaxField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// MIN() - 返回最小值
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted">fieldName 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> MinField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// SUM() - 返回总和
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted">fieldName 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> SumField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// AVG() - 返回平均值
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted">fieldName 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> AvgField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// COUNT() - 返回行数
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="aliasName">别名</param>
        /// <param name="fieldNameFormatted">fieldName 是否已经被格式化处理</param>
        /// <returns></returns>
        IQueryable<TEntity> CountField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null, bool fieldNameFormatted = false);
        /// <summary>
        /// SELECT COUNT(DISTINCT field_name) FROM table_name;
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="aliasName">别名</param>
        /// <returns></returns>
        IQueryable<TEntity> CountDistinctField(Expression<Func<TEntity, object>> fieldExpression, string aliasName = null);
        /// <summary>
        /// SELECT DISTINCT field_name1,field_name2 FROM table_name;
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IQueryable<TEntity> DistinctFields(Expression<Func<TEntity, object>> fieldExpression);
        #endregion

        #region [Join] 表关联
        /// <summary>
        /// INNER JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：INNER JOIN</param>
        /// <returns></returns>
        IQueryable<TEntity> InnerJoin(string joinTableSql);
        /// <summary>
        /// LEFT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：LEFT JOIN</param>
        /// <returns></returns>
        IQueryable<TEntity> LeftJoin(string joinTableSql);
        /// <summary>
        /// RIGHT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：RIGHT JOIN</param>
        /// <returns></returns>
        IQueryable<TEntity> RightJoin(string joinTableSql);
        /// <summary>
        /// FULL JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：FULL JOIN</param>
        /// <returns></returns>
        IQueryable<TEntity> FullJoin(string joinTableSql);

        /// <summary>
        /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IQueryable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IQueryable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IQueryable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IQueryable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        #endregion

        #region [WHERE]
        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="where">不包含关键字：WHERE</param>
        /// <returns></returns>
        IQueryable<TEntity> Where(string where);
        /// <summary>
        /// 解析WHERE过滤条件
        /// </summary>
        /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
        /// <returns></returns>
        IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
        IQueryable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="operation"></param>
        /// <param name="keyword"></param>
        /// <param name="include"></param>
        /// <param name="paramName">参数名称，默认同 <paramref name="fieldName"/></param>
        /// <returns></returns>
        IQueryable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
        IQueryable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

        IQueryable<TEntity> AndWhere(string where);
        IQueryable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
        IQueryable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
        #endregion

        #region [GROUP BY]
        /// <summary>
        /// GROUP BY column_name
        /// </summary>
        /// <param name="groupBy">不包含关键字：GROUP BY</param>
        /// <returns></returns>
        IQueryable<TEntity> GroupBy(string groupBy);
        IQueryable<TEntity> GroupByField(params string[] fieldNames);
        IQueryable<TEntity> GroupByField(Expression<Func<TEntity, object>> fieldExpression);
        #endregion

        #region [HAVING]
        /// <summary>
        /// HAVING aggregate_function(column_name) operator value
        /// </summary>
        /// <param name="having">不包含关键字：HAVING</param>
        /// <returns></returns>
        IQueryable<TEntity> Having(string having);
        #endregion

        #region [ORDER BY]
        /// <summary>
        /// ORDER BY column_name,column_name ASC|DESC;
        /// </summary>
        /// <param name="orderBy">不包含关键字：ORDER BY</param>
        /// <returns></returns>
        IQueryable<TEntity> OrderBy(string orderBy);
        IQueryable<TEntity> OrderBy(OrderByCondition orderBy);
        IQueryable<TEntity> OrderByField(OrderByType type, params string[] fieldNames);
        IQueryable<TEntity> OrderByField(OrderByType type, Expression<Func<TEntity, object>> fieldExpression);
        #endregion

        /// <summary>
        /// 设置 TOP 查询参数
        /// <para>SELECT TOP {<paramref name="top"/>}</para>
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        IQueryable<TEntity> Top(int? top);
        /// <summary>
        /// 设置分页查询参数
        /// <para>LIMIT {(<paramref name="pageIndex"/> - 1) * <paramref name="pageSize"/>},{<paramref name="pageSize"/>}</para>
        /// </summary>
        /// <param name="pageIndex">The current page index for paging query, the minimum value is 1.</param>
        /// <param name="pageSize">The page size for paging query.</param>
        /// <returns></returns>
        IQueryable<TEntity> Page(int? pageIndex, int? pageSize);
        /// <summary>
        /// 设置偏移量查询参数
        /// <para>LIMIT {<paramref name="offset"/>},{<paramref name="rows"/>}</para>
        /// </summary>
        /// <param name="offset">Offset to use for this query.</param>
        /// <param name="rows">The number of rows queried.</param>
        /// <returns></returns>
        IQueryable<TEntity> Offset(int? offset, int? rows);

        /// <summary>
        /// 设置SQL入参
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        IQueryable<TEntity> SetParameter(object param);
    }
}