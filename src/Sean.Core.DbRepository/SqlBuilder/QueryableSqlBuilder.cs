using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;

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
        private readonly List<string> _includeFieldsList = new();

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
        /// <param name="dbType"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
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
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field) && !_includeFieldsList.Contains(field))
                    {
                        _includeFieldsList.Add(field);
                    }
                }
            }
            return this;
        }
        public virtual IQueryable<TEntity> IgnoreFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field) && _includeFieldsList.Contains(field))
                    {
                        _includeFieldsList.Remove(field);
                    }
                }
            }
            return this;
        }

        public virtual IQueryable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression, TEntity entity = default)
        {
            if (fieldExpression == null)
            {
                if (entity != null)
                {
                    SetParameter(entity);
                }
                return this;
            }

            var fields = fieldExpression.GetMemberNames().ToArray();
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
        public virtual IQueryable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IgnoreFields(fields);
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
            var aqlAdapter = new DefaultSqlAdapter(SqlAdapter.DbType, typeof(TEntity2).GetMainTableName())
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
            var aqlAdapter = new DefaultSqlAdapter(SqlAdapter.DbType, typeof(TEntity2).GetMainTableName())
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
            var aqlAdapter = new DefaultSqlAdapter(SqlAdapter.DbType, typeof(TEntity2).GetMainTableName())
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
            if (fieldNames != null)
            {
                if (MultiTable)
                {
                    SqlAdapter.MultiTable = true;
                }
                GroupBy(string.Join(", ", fieldNames.Select(fieldName => SqlAdapter.FormatFieldName(fieldName))));
            }
            return this;
        }
        public virtual IQueryable<TEntity> GroupByField<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return GroupByField(fieldExpression.GetMemberNames().ToArray());
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
                if (_orderBy.Value.Length > 0) this._orderBy.Value.Append(" ");
                this._orderBy.Value.Append(orderBy);
            }
            return this;
        }
        public virtual IQueryable<TEntity> OrderBy(OrderByCondition orderByCondition)
        {
            if (orderByCondition != null)
            {
                orderByCondition.Resolve((type, fields) => OrderByField(type, fields));
            }
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
        public virtual IQueryable<TEntity> OrderByField<TProperty>(OrderByType type, Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return OrderByField(type, fieldExpression.GetMemberNames().ToArray());
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

        public virtual IQueryableSql Build()
        {
            if (MultiTable)
            {
                SqlAdapter.MultiTable = true;
            }

            var deleteableSql = new DefaultQueryableSql();
            var selectFields = _includeFieldsList.Any() ? string.Join(", ", _includeFieldsList.Select(fieldName => $"{SqlAdapter.FormatFieldName(fieldName)}")) : "*";
            //const string rowNumAlias = "ROW_NUM";
            if (_topNumber.HasValue)
            {
                // 查询前几行
                switch (SqlAdapter.DbType)
                {
                    case DatabaseType.MySql:
                    case DatabaseType.SQLite:
                    case DatabaseType.PostgreSql:
                        deleteableSql.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber};";
                        break;
                    case DatabaseType.SqlServer:
                    case DatabaseType.SqlServerCe:
                    case DatabaseType.Access:
                        deleteableSql.QuerySql = $"SELECT TOP {_topNumber} {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                        break;
                    case DatabaseType.Oracle:
                        var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {_topNumber}" : $"{WhereSql} AND ROWNUM <= {_topNumber}";
                        deleteableSql.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}{OrderBySql};";
                        break;
                    default:
                        //throw new NotSupportedException($"[{nameof(QuerySql)}]-[{_dbType}]-[{nameof(TopNumber)}:{TopNumber}]");
                        deleteableSql.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber};";// 同MySql
                        break;
                }
            }
            else if (_pageIndex.HasValue && _pageSize.HasValue)
            {
                // 分页查询
                var offset = (_pageIndex.Value - 1) * _pageSize.Value;// 偏移量
                var rows = _pageSize.Value;// 行数
                deleteableSql.QuerySql = GetQuerySql(selectFields, offset, rows);
            }
            else if (_offset.HasValue && _rows.HasValue)
            {
                // 根据偏移量查询
                deleteableSql.QuerySql = GetQuerySql(selectFields, _offset.Value, _rows.Value);
            }
            else
            {
                // 普通查询
                deleteableSql.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
            }

            deleteableSql.Parameter = _parameter;
            return deleteableSql;
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
                case DatabaseType.SqlServerCe:
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} OFFSET {offset} ROWS FETCH NEXT {rows} ROWS ONLY;";
                case DatabaseType.DB2:
                    // SQL Server、Oracle等数据库都支持：ROW_NUMBER() OVER()
                    return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset} AND t2.ROW_NUM <= {offset + rows};";
                case DatabaseType.Access:
                    return $"SELECT TOP {rows} {selectFields} FROM (SELECT TOP {offset + rows} {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}) t2;";
                case DatabaseType.Oracle:
                    if (string.IsNullOrWhiteSpace(OrderBySql))
                    {
                        // 无ORDER BY排序
                        // SQL示例：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 AND ROWNUM<=10) t2 WHERE t2.ROW_NUM>5;
                        var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {offset + rows}" : $"{WhereSql} AND ROWNUM <= {offset + rows}";
                        return $"SELECT {selectFields} FROM (SELECT ROWNUM ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset};";
                    }
                    else
                    {
                        // 有ORDER BY排序
                        // SQL示例1：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM (SELECT ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 ORDER BY ID DESC) t2 WHERE ROWNUM<=10) t3 WHERE t3.ROW_NUM>5
                        // SQL示例2：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROW_NUMBER() OVER(ORDER BY ID DESC) ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456) t2 WHERE t2.ROW_NUM>5 AND t2.ROW_NUM<=10;
                        return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset} AND t2.ROW_NUM <= {offset + rows};";
                    }
                default:
                    //throw new NotSupportedException($"[{nameof(QuerySql)}]-[{_dbType}]-[{nameof(PageIndex)}:{PageIndex},{nameof(PageSize)}:{PageSize}]");
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName()}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows};";// 同MySql
            }
        }
    }

    public interface IQueryable
    {
        /// <summary>
        /// 创建SQL：查询数据
        /// </summary>
        /// <returns></returns>
        IQueryableSql Build();
    }

    public interface IQueryable<TEntity> : IQueryable
    {
        #region [Field]
        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IQueryable<TEntity> IncludeFields(params string[] fields);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IQueryable<TEntity> IgnoreFields(params string[] fields);

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        IQueryable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression, TEntity entity = default);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IQueryable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
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
        /// <param name="whereExpression"></param>
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
        IQueryable<TEntity> GroupByField<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
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
        IQueryable<TEntity> OrderBy(OrderByCondition orderByCondition);
        IQueryable<TEntity> OrderByField(OrderByType type, params string[] fieldNames);
        IQueryable<TEntity> OrderByField<TProperty>(OrderByType type, Expression<Func<TEntity, TProperty>> fieldExpression);
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
        /// <param name="pageIndex">分页参数：当前页号（最小值为1）</param>
        /// <param name="pageSize">分页参数：页大小</param>
        /// <returns></returns>
        IQueryable<TEntity> Page(int? pageIndex, int? pageSize);
        /// <summary>
        /// 设置偏移量查询参数
        /// <para>LIMIT {<paramref name="offset"/>},{<paramref name="rows"/>}</para>
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <param name="rows">行数</param>
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