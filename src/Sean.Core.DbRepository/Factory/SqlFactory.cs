using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public class SqlFactory : IInsertableSql, IDeleteableSql, IUpdateableSql, IQueryableSql, ICountableSql
    {
        #region SQL
        /// <summary>
        /// SQL：新增数据
        /// </summary>
        public virtual string InsertSql { get; private set; }
        /// <summary>
        /// SQL：删除数据
        /// </summary>
        public virtual string DeleteSql { get; private set; }
        /// <summary>
        /// SQL：更新数据
        /// </summary>
        public virtual string UpdateSql { get; private set; }
        /// <summary>
        /// SQL：查询数据
        /// </summary>
        public virtual string QuerySql { get; private set; }
        /// <summary>
        /// SQL：统计数量
        /// </summary>
        public virtual string CountSql { get; private set; }
        #endregion

        public ISqlAdapter SqlAdapter { get; }

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// 获取SQL入参
        /// </summary>
        public object Parameter { get; private set; }

        /// <summary>
        /// 包含字段
        /// </summary>
        private readonly List<string> _includeFieldsList = new();
        /// <summary>
        /// 忽略字段
        /// </summary>
        private readonly List<string> _ignoreFieldsList = new();
        /// <summary>
        /// 自增字段
        /// </summary>
        private readonly List<string> _identityFieldsList = new();

        protected string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
        protected string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;
        protected string GroupBySql => _groupBy.IsValueCreated && _groupBy.Value.Length > 0 ? $" GROUP BY {_groupBy.Value.ToString()}" : string.Empty;
        protected string HavingSql => _having.IsValueCreated && _having.Value.Length > 0 ? $" HAVING {_having.Value.ToString()}" : string.Empty;
        protected string OrderBySql => _orderBy.IsValueCreated && _orderBy.Value.Length > 0 ? $" ORDER BY {_orderBy.Value.ToString()}" : string.Empty;

        /// <summary>
        /// 是否是表关联查询
        /// </summary>
        protected bool MultiTableQuery => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

        private readonly Lazy<StringBuilder> _joinTable = new();
        private readonly Lazy<StringBuilder> _where = new();
        private readonly Lazy<StringBuilder> _groupBy = new();
        private readonly Lazy<StringBuilder> _having = new();
        private readonly Lazy<StringBuilder> _orderBy = new();
        private bool _returnLastInsertId;
        protected bool _allowEmptyWhereClause;
        protected int? _bulkInsertEntityCount;
        private int? _topNumber;
        private int? _pageIndex;
        private int? _pageSize;
        private int? _offset;
        private int? _rows;
        private Type _tableEntityType;

        private Func<string, ISqlAdapter, string> _fieldCustomHandler;

        protected SqlFactory(DatabaseType dbType, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            SqlAdapter = new DefaultSqlAdapter(dbType);
            TableName = tableName;
        }

        public static SqlFactory Create(DatabaseType dbType, string tableName)
        {
            return new(dbType, tableName);
        }

        #region BuildSql
        /// <summary>
        /// 创建SQL：新增数据
        /// </summary>
        /// <returns></returns>
        public virtual IInsertableSql BuildInsertableSql()
        {
            var list = _includeFieldsList.Except(_identityFieldsList).ToList();
            if (!list.Any())
                return this;

            var fields = list.Select(fieldName => FormatFieldName(fieldName));
            var sb = new StringBuilder($"INSERT INTO {SqlAdapter.FormatTableName(TableName)}({string.Join(", ", fields)}) VALUES");
            if (!_bulkInsertEntityCount.HasValue)
            {
                var parameters = list.Select(fieldName => SqlAdapter.FormatInputParameter(fieldName));
                sb.Append($"({string.Join(", ", parameters)});");
            }
            else
            {
                var bulkInsertEntityCount = _bulkInsertEntityCount.Value;
                var listFieldInsert = new List<string>();
                for (int i = 0; i < bulkInsertEntityCount; i++)
                {
                    var parameters = list.Select(fieldName => SqlAdapter.FormatInputParameter($"{fieldName}{i + 1}"));
                    listFieldInsert.Add($"({string.Join(", ", parameters)})");
                }
                sb.Append(string.Join(",", listFieldInsert));
                sb.Append(";");
            }

            if (_returnLastInsertId)
            {
                switch (SqlAdapter.DbType)
                {
                    case DatabaseType.Oracle:
                        var sequence = TypeCache.GetEntityInfo(_tableEntityType)?.Sequence;
                        sb.Append(string.Format(SqlAdapter.GetSqlForSelectLastInsertId(), sequence));
                        break;
                    default:
                        sb.Append(SqlAdapter.GetSqlForSelectLastInsertId());
                        break;
                }
            }
            this.InsertSql = sb.ToString();
            return this;
        }
        /// <summary>
        /// 创建SQL：删除数据（为了防止误删除，需要指定WHERE过滤条件，否则会抛出异常，可以通过 <see cref="AllowEmptyWhereClause"/> 设置允许空 WHERE 子句）
        /// </summary>
        /// <returns></returns>
        public virtual IDeleteableSql BuildDeleteableSql()
        {
            if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

            this.DeleteSql = $"DELETE FROM {SqlAdapter.FormatTableName(TableName)}{WhereSql};";
            return this;
        }
        /// <summary>
        /// 创建SQL：更新数据（为了防止误更新，需要指定WHERE过滤条件，否则会抛出异常，可以通过 <see cref="AllowEmptyWhereClause"/> 设置允许空 WHERE 子句）
        /// </summary>
        /// <returns></returns>
        public virtual IUpdateableSql BuildUpdateableSql()
        {
            if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

            var list = _includeFieldsList.Except(_identityFieldsList).ToList();
            if (!list.Any())
                return this;

            var sets = _fieldCustomHandler != null
                ? list.Select(fieldName => _fieldCustomHandler(fieldName, SqlAdapter))
                : list.Select(fieldName => $"{FormatFieldName(fieldName)}={SqlAdapter.FormatInputParameter(fieldName)}");
            this.UpdateSql = $"UPDATE {SqlAdapter.FormatTableName(TableName)} SET {string.Join(", ", sets)}{WhereSql};";
            return this;
        }
        /// <summary>
        /// 创建SQL：查询数据
        /// </summary>
        /// <returns></returns>
        public virtual IQueryableSql BuildQueryableSql()
        {
            var selectFields = _includeFieldsList.Any() ? string.Join(", ", _includeFieldsList.Select(fieldName => $"{FormatFieldName(fieldName)}")) : "*";
            //const string rowNumAlias = "ROW_NUM";
            if (_topNumber.HasValue)
            {
                // 查询前几行
                switch (SqlAdapter.DbType)
                {
                    case DatabaseType.MySql:
                    case DatabaseType.SQLite:
                    case DatabaseType.PostgreSql:
                        this.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber};";
                        break;
                    case DatabaseType.SqlServer:
                    case DatabaseType.SqlServerCe:
                    case DatabaseType.Access:
                        this.QuerySql = $"SELECT TOP {_topNumber} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                        break;
                    case DatabaseType.Oracle:
                        var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {_topNumber}" : $"{WhereSql} AND ROWNUM <= {_topNumber}";
                        this.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}{OrderBySql};";
                        break;
                    default:
                        //throw new NotSupportedException($"[{nameof(QuerySql)}]-[{_dbType}]-[{nameof(TopNumber)}:{TopNumber}]");
                        this.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNumber};";// 同MySql
                        break;
                }
            }
            else if (_pageIndex.HasValue && _pageSize.HasValue)
            {
                // 分页查询
                var offset = (_pageIndex.Value - 1) * _pageSize.Value;// 偏移量
                var rows = _pageSize.Value;// 行数
                this.QuerySql = GetQuerySql(selectFields, offset, rows);
            }
            else if (_offset.HasValue && _rows.HasValue)
            {
                // 根据偏移量查询
                this.QuerySql = GetQuerySql(selectFields, _offset.Value, _rows.Value);
            }
            else
            {
                // 普通查询
                this.QuerySql = $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
            }
            return this;
        }
        /// <summary>
        /// 创建SQL：统计数量
        /// </summary>
        /// <returns></returns>
        public virtual ICountableSql BuildCountableSql()
        {
            this.CountSql = $"SELECT COUNT(1) FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql};";
            return this;
        }
        #endregion

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public virtual SqlFactory IncludeFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field) && !_includeFieldsList.Contains(field))
                    {
                        _includeFieldsList.Add(field);
                        if (_ignoreFieldsList.Contains(field))
                        {
                            _ignoreFieldsList.Remove(field);
                        }
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public virtual SqlFactory IgnoreFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field) && !_ignoreFieldsList.Contains(field))
                    {
                        _ignoreFieldsList.Add(field);
                        if (_includeFieldsList.Contains(field))
                        {
                            _includeFieldsList.Remove(field);
                        }
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// 自增字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public virtual SqlFactory IdentityFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field) && !_identityFieldsList.Contains(field))
                    {
                        _identityFieldsList.Add(field);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// 设置 TOP 查询参数
        /// <para>SELECT TOP {<paramref name="top"/>}</para>
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public virtual SqlFactory Top(int? top)
        {
            this._topNumber = top;
            return this;
        }
        /// <summary>
        /// 设置分页查询参数
        /// <para>LIMIT {(<paramref name="pageIndex"/> - 1) * <paramref name="pageSize"/>},{<paramref name="pageSize"/>}</para>
        /// </summary>
        /// <param name="pageIndex">分页参数：当前页号（最小值为1）</param>
        /// <param name="pageSize">分页参数：页大小</param>
        /// <returns></returns>
        public virtual SqlFactory Page(int? pageIndex, int? pageSize)
        {
            this._pageIndex = pageIndex;
            this._pageSize = pageSize;
            return this;
        }
        /// <summary>
        /// 设置偏移量查询参数
        /// <para>LIMIT {<paramref name="offset"/>},{<paramref name="rows"/>}</para>
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <param name="rows">行数</param>
        /// <returns></returns>
        public virtual SqlFactory Offset(int? offset, int? rows)
        {
            this._offset = offset;
            this._rows = rows;
            return this;
        }

        #region 表关联查询
        /// <summary>
        /// INNER JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：INNER JOIN</param>
        /// <returns></returns>
        public virtual SqlFactory InnerJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" INNER JOIN {joinTableSql}");
            }
            return this;
        }
        /// <summary>
        /// LEFT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：LEFT JOIN</param>
        /// <returns></returns>
        public virtual SqlFactory LeftJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
            }
            return this;
        }
        /// <summary>
        /// RIGHT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：RIGHT JOIN</param>
        /// <returns></returns>
        public virtual SqlFactory RightJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
            }
            return this;
        }
        /// <summary>
        /// FULL JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：FULL JOIN</param>
        /// <returns></returns>
        public virtual SqlFactory FullJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" FULL JOIN {joinTableSql}");
            }
            return this;
        }
        #endregion

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="where">不包含关键字：WHERE</param>
        /// <returns></returns>
        public virtual SqlFactory Where(string where)
        {
            if (!string.IsNullOrWhiteSpace(where))
            {
                if (_where.Value.Length > 0) this._where.Value.Append(" ");
                this._where.Value.Append(where);
            }
            return this;
        }
        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="operation"></param>
        /// <param name="keyword"></param>
        /// <param name="include"></param>
        /// <param name="paramName">参数名称，默认同 <paramref name="fieldName"/></param>
        /// <returns></returns>
        public virtual SqlFactory WhereField(string fieldName, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                if (_where.Value.Length > 0) this._where.Value.Append(" ");
                else if (keyword == WhereSqlKeyword.And) this._where.Value.Append("1=1 ");

                var keywordSqlString = keyword.ToSqlString();
                if (!string.IsNullOrWhiteSpace(keywordSqlString))
                {
                    this._where.Value.Append($"{keywordSqlString} ");
                }

                if (include == Include.Left)
                {
                    this._where.Value.Append(include.ToSqlString());
                }
                this._where.Value.Append($"{FormatFieldName(fieldName)} {operation.ToSqlString()} {SqlAdapter.FormatInputParameter(!string.IsNullOrWhiteSpace(paramName) ? paramName : fieldName)}");
                if (include == Include.Right)
                {
                    this._where.Value.Append(include.ToSqlString());
                }
            }
            return this;
        }
        /// <summary>
        /// GROUP BY column_name
        /// </summary>
        /// <param name="groupBy">不包含关键字：GROUP BY</param>
        /// <returns></returns>
        public virtual SqlFactory GroupBy(string groupBy)
        {
            if (!string.IsNullOrWhiteSpace(groupBy))
            {
                if (_groupBy.Value.Length > 0) this._groupBy.Value.Append(" ");
                this._groupBy.Value.Append(groupBy);
            }
            return this;
        }
        public virtual SqlFactory GroupByField(params string[] fieldNames)
        {
            if (fieldNames != null)
            {
                GroupBy(string.Join(", ", fieldNames.Select(fieldName => FormatFieldName(fieldName))));
            }
            return this;
        }
        /// <summary>
        /// HAVING aggregate_function(column_name) operator value
        /// </summary>
        /// <param name="having">不包含关键字：HAVING</param>
        /// <returns></returns>
        public virtual SqlFactory Having(string having)
        {
            if (!string.IsNullOrWhiteSpace(having))
            {
                if (_having.Value.Length > 0) this._having.Value.Append(" ");
                this._having.Value.Append(having);
            }
            return this;
        }
        /// <summary>
        /// ORDER BY column_name,column_name ASC|DESC;
        /// </summary>
        /// <param name="orderBy">不包含关键字：ORDER BY</param>
        /// <returns></returns>
        public virtual SqlFactory OrderBy(string orderBy)
        {
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                if (_orderBy.Value.Length > 0) this._orderBy.Value.Append(" ");
                this._orderBy.Value.Append(orderBy);
            }
            return this;
        }
        public virtual SqlFactory OrderBy(OrderByCondition orderByCondition)
        {
            if (orderByCondition != null)
            {
                orderByCondition.Resolve(this);
            }
            return this;
        }
        public virtual SqlFactory OrderByField(OrderByType type, params string[] fieldNames)
        {
            if (fieldNames != null && fieldNames.Any())
            {
                if (_orderBy.Value.Length > 0) this._orderBy.Value.Append(", ");

                _orderBy.Value.Append($"{string.Join(", ", fieldNames.Select(fieldName => FormatFieldName(fieldName)))} {type.ToSqlString()}");
            }
            return this;
        }

        /// <summary>
        /// 是否返回自增主键id（仅用于 <see cref="InsertSql"/>）
        /// </summary>
        /// <param name="returnLastInsertId"></param>
        /// <returns></returns>
        public virtual SqlFactory ReturnLastInsertId(bool returnLastInsertId)
        {
            this._returnLastInsertId = returnLastInsertId;
            return this;
        }

        /// <summary>
        /// 是否允许空的WHERE子句（适用于：<see cref="DeleteSql"/>、<see cref="UpdateSql"/>）
        /// <para>注：为了防止执行错误的SQL导致不可逆的结果，默认不允许空的WHERE子句</para>
        /// </summary>
        /// <param name="allowEmptyWhereClause"></param>
        /// <returns></returns>
        public virtual SqlFactory AllowEmptyWhereClause(bool allowEmptyWhereClause)
        {
            this._allowEmptyWhereClause = allowEmptyWhereClause;
            return this;
        }

        /// <summary>
        /// 设置SQL入参
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual SqlFactory SetParameter(object param)
        {
            this.Parameter = param;
            return this;
        }

        public virtual SqlFactory SetFieldCustomHandler(Func<string, ISqlAdapter, string> fieldCustomHandler)
        {
            _fieldCustomHandler = fieldCustomHandler;
            return this;
        }

        protected virtual SqlFactory Table<TEntity>()
        {
            this._tableEntityType = typeof(TEntity);
            return this;
        }

        protected virtual string FormatFieldName(string fieldName, string tableName = null)
        {
            if (MultiTableQuery || !string.IsNullOrWhiteSpace(tableName))
            {
                return $"{SqlAdapter.FormatTableName(tableName ?? TableName)}.{SqlAdapter.FormatFieldName(fieldName)}";
            }

            return SqlAdapter.FormatFieldName(fieldName);
        }

        private string GetQuerySql(string selectFields, int offset, int rows)
        {
            switch (SqlAdapter.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows};";
                case DatabaseType.PostgreSql:
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {rows} OFFSET {offset};";
                case DatabaseType.SqlServer:
                case DatabaseType.SqlServerCe:
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} OFFSET {offset} ROWS FETCH NEXT {rows} ROWS ONLY;";
                case DatabaseType.DB2:
                    // SQL Server、Oracle等数据库都支持：ROW_NUMBER() OVER()
                    return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset} AND t2.ROW_NUM <= {offset + rows};";
                case DatabaseType.Access:
                    return $"SELECT TOP {rows} {selectFields} FROM (SELECT TOP {offset + rows} {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}) t2;";
                case DatabaseType.Oracle:
                    if (string.IsNullOrWhiteSpace(OrderBySql))
                    {
                        // 无ORDER BY排序
                        // SQL示例：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 AND ROWNUM<=10) t2 WHERE t2.ROW_NUM>5;
                        var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {offset + rows}" : $"{WhereSql} AND ROWNUM <= {offset + rows}";
                        return $"SELECT {selectFields} FROM (SELECT ROWNUM ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{sqlWhere}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset};";
                    }
                    else
                    {
                        // 有ORDER BY排序
                        // SQL示例1：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM (SELECT ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 ORDER BY ID DESC) t2 WHERE ROWNUM<=10) t3 WHERE t3.ROW_NUM>5
                        // SQL示例2：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROW_NUMBER() OVER(ORDER BY ID DESC) ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456) t2 WHERE t2.ROW_NUM>5 AND t2.ROW_NUM<=10;
                        return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {offset} AND t2.ROW_NUM <= {offset + rows};";
                    }
                default:
                    //throw new NotSupportedException($"[{nameof(QuerySql)}]-[{_dbType}]-[{nameof(PageIndex)}:{PageIndex},{nameof(PageSize)}:{PageSize}]");
                    return $"SELECT {selectFields} FROM {SqlAdapter.FormatTableName(TableName)}{JoinTableSql}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {offset},{rows};";// 同MySql
            }
        }
    }

    public class SqlFactory<TEntity> : SqlFactory
    {
        private SqlFactory(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
            base.Table<TEntity>();
        }

        /// <summary>
        /// 创建新的 <see cref="SqlFactory{TEntity}"/> 实例
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="tableName"></param>
        /// <param name="autoIncludeFields">是否自动包含字段（反射解析实体类）：
        /// <para><see cref="SqlFactory.IncludeFields"/></para>
        /// <para><see cref="SqlFactory.IdentityFields"/></para>
        /// </param>
        /// <returns></returns>
        public static SqlFactory<TEntity> Create(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            var sqlFactory = new SqlFactory<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
            if (autoIncludeFields)
            {
                sqlFactory.IncludeFields(typeof(TEntity).GetAllFieldNames().ToArray());
                sqlFactory.IdentityFields(typeof(TEntity).GetIdentityFieldNames().ToArray());
            }
            return sqlFactory;
        }
        /// <summary>
        /// 创建新的 <see cref="SqlFactory{TEntity}"/> 实例
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields">是否自动包含字段（反射解析实体类）：
        /// <para><see cref="SqlFactory.IncludeFields"/></para>
        /// <para><see cref="SqlFactory.IdentityFields"/></para>
        /// </param>
        /// <returns></returns>
        public static SqlFactory<TEntity> Create(IBaseRepository repository, bool autoIncludeFields)
        {
            return Create(repository.DbType, autoIncludeFields, repository.TableName());
        }

        #region override methods
        /// <summary>
        /// 创建SQL：删除数据（如果没有指定WHERE过滤条件，且没有设置 <see cref="AllowEmptyWhereClause"/> 为true，则过滤条件默认使用 <see cref="KeyAttribute"/> 主键字段）
        /// </summary>
        /// <returns></returns>
        public override IDeleteableSql BuildDeleteableSql()
        {
            if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
            {
                typeof(TEntity).GetPrimaryKeys().ForEach(fieldName => WhereField(fieldName, SqlOperation.Equal));
            }

            return base.BuildDeleteableSql();
        }
        /// <summary>
        /// 创建SQL：更新数据（如果没有指定WHERE过滤条件，且没有设置 <see cref="AllowEmptyWhereClause"/> 为true，则过滤条件默认使用 <see cref="KeyAttribute"/> 主键字段）
        /// </summary>
        /// <returns></returns>
        public override IUpdateableSql BuildUpdateableSql()
        {
            if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
            {
                typeof(TEntity).GetPrimaryKeys().ForEach(fieldName => WhereField(fieldName, SqlOperation.Equal));
            }

            return base.BuildUpdateableSql();
        }

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> IncludeFields(params string[] fields)
        {
            return base.IncludeFields(fields) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> IgnoreFields(params string[] fields)
        {
            return base.IgnoreFields(fields) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// 自增字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> IdentityFields(params string[] fields)
        {
            return base.IdentityFields(fields) as SqlFactory<TEntity>;
        }

        /// <summary>
        /// 设置 TOP 查询参数
        /// <para>SELECT TOP {<paramref name="top"/>}</para>
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Top(int? top)
        {
            return base.Top(top) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// 设置分页查询参数
        /// <para>LIMIT {(<paramref name="pageIndex"/> - 1) * <paramref name="pageSize"/>},{<paramref name="pageSize"/>}</para>
        /// </summary>
        /// <param name="pageIndex">分页参数：当前页号（最小值为1）</param>
        /// <param name="pageSize">分页参数：页大小</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Page(int? pageIndex, int? pageSize)
        {
            return base.Page(pageIndex, pageSize) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// 设置偏移量查询参数
        /// <para>LIMIT {<paramref name="offset"/>},{<paramref name="rows"/>}</para>
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <param name="rows">行数</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Offset(int? offset, int? rows)
        {
            return base.Offset(offset, rows) as SqlFactory<TEntity>;
        }

        #region 表关联查询
        /// <summary>
        /// INNER JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：INNER JOIN</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> InnerJoin(string joinTableSql)
        {
            return base.InnerJoin(joinTableSql) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// LEFT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：LEFT JOIN</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> LeftJoin(string joinTableSql)
        {
            return base.LeftJoin(joinTableSql) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// RIGHT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：RIGHT JOIN</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> RightJoin(string joinTableSql)
        {
            return base.RightJoin(joinTableSql) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// FULL JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：FULL JOIN</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> FullJoin(string joinTableSql)
        {
            return base.FullJoin(joinTableSql) as SqlFactory<TEntity>;
        }
        #endregion

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="where">不包含关键字：WHERE</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Where(string where)
        {
            return base.Where(where) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="operation"></param>
        /// <param name="keyword"></param>
        /// <param name="include"></param>
        /// <param name="paramName">参数名称，默认同 <paramref name="fieldName"/></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> WhereField(string fieldName, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            return base.WhereField(fieldName, operation, keyword, include, paramName) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// GROUP BY column_name
        /// </summary>
        /// <param name="groupBy">不包含关键字：GROUP BY</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> GroupBy(string groupBy)
        {
            return base.GroupBy(groupBy) as SqlFactory<TEntity>;
        }
        public new virtual SqlFactory<TEntity> GroupByField(params string[] fieldNames)
        {
            return base.GroupByField(fieldNames) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// HAVING aggregate_function(column_name) operator value
        /// </summary>
        /// <param name="having">不包含关键字：HAVING</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Having(string having)
        {
            return base.Having(having) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// ORDER BY column_name,column_name ASC|DESC;
        /// </summary>
        /// <param name="orderBy">不包含关键字：ORDER BY</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> OrderBy(string orderBy)
        {
            return base.OrderBy(orderBy) as SqlFactory<TEntity>;
        }
        public new virtual SqlFactory<TEntity> OrderBy(OrderByCondition orderByCondition)
        {
            return base.OrderBy(orderByCondition) as SqlFactory<TEntity>;
        }
        public new virtual SqlFactory<TEntity> OrderByField(OrderByType type, params string[] fieldNames)
        {
            return base.OrderByField(type, fieldNames) as SqlFactory<TEntity>;
        }

        /// <summary>
        /// 是否返回自增主键id（仅用于 <see cref="SqlFactory.InsertSql"/>）
        /// </summary>
        /// <param name="returnLastInsertId"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> ReturnLastInsertId(bool returnLastInsertId)
        {
            return base.ReturnLastInsertId(returnLastInsertId) as SqlFactory<TEntity>;
        }

        /// <summary>
        /// 是否允许空的WHERE子句（适用于：<see cref="SqlFactory.DeleteSql"/>、<see cref="SqlFactory.UpdateSql"/>）
        /// <para>注：为了防止执行错误的SQL导致不可逆的结果，默认不允许空的WHERE子句</para>
        /// </summary>
        /// <param name="allowEmptyWhereClause"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause)
        {
            return base.AllowEmptyWhereClause(allowEmptyWhereClause) as SqlFactory<TEntity>;
        }

        /// <summary>
        /// 设置SQL入参
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> SetParameter(object param)
        {
            return base.SetParameter(param) as SqlFactory<TEntity>;
        }

        public new virtual SqlFactory<TEntity> SetFieldCustomHandler(Func<string, ISqlAdapter, string> fieldCustomHandler)
        {
            return base.SetFieldCustomHandler(fieldCustomHandler) as SqlFactory<TEntity>;
        }
        #endregion

        public Dictionary<string, object> ConvertToParameter(object instance, string[] fields = null)
        {
            if (instance == null) return null;

            var paramDic = new Dictionary<string, object>();
            if (fields != null && fields.Any())
            {
                // 指定字段
                foreach (var field in fields)
                {
                    var propertyInfo = instance.GetType().GetProperty(field);
                    if (propertyInfo == null)
                    {
                        throw new InvalidOperationException($"在[{typeof(TEntity).FullName}]中未找到公共属性：{field}");
                    }

                    paramDic.Add(propertyInfo.Name, propertyInfo.GetValue(instance, null));
                }
            }
            else
            {
                // 所有字段
                foreach (var fieldInfo in typeof(TEntity).GetEntityInfo().FieldInfos)
                {
                    paramDic.Add(fieldInfo.FieldName, fieldInfo.Property.GetValue(instance, null));
                }
            }
            return paramDic;
        }

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression, TEntity entity = default)
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
                var paramDic = ConvertToParameter(entity, null); //ConvertToParameter(entity, fields);
                if (paramDic != null && paramDic.Any())
                {
                    SetParameter(paramDic);
                }
            }

            return this;
        }
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IgnoreFields(fields);
        }
        /// <summary>
        /// 自增字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> IdentityFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IdentityFields(fields);
        }

        #region 表关联查询
        /// <summary>
        /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return InnerJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {GetJoinFields(fieldExpression, fieldExpression2, joinTableName)}");
        }
        /// <summary>
        /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return LeftJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {GetJoinFields(fieldExpression, fieldExpression2, joinTableName)}");
        }
        /// <summary>
        /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return RightJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {GetJoinFields(fieldExpression, fieldExpression2, joinTableName)}");
        }
        /// <summary>
        /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return FullJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {GetJoinFields(fieldExpression, fieldExpression2, joinTableName)}");
        }

        private string GetJoinFields<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2, string joinTableName)
        {
            var fields = fieldExpression.GetMemberNames();
            var fields2 = fieldExpression2.GetMemberNames();
            if (!fields.Any())
            {
                throw new InvalidOperationException("The specified number of fields must be greater than 0.");
            }
            if (fields.Count != fields2.Count)
            {
                throw new InvalidOperationException("The specified number of fields must be equal.");
            }

            var list = new List<string>();
            for (var i = 0; i < fields.Count; i++)
            {
                list.Add($"{FormatFieldName(fields[i], TableName)}={FormatFieldName(fields2[i], joinTableName)}");
            }

            return string.Join(" AND ", list);
        }
        #endregion

        /// <summary>
        /// 解析WHERE过滤条件
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
        {
            if (whereExpression == null) return this;
            return whereExpression.ResolveWhereExpression(this);
        }

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="operation"></param>
        /// <param name="keyword"></param>
        /// <param name="include"></param>
        /// <param name="paramName">参数名称，默认同 <paramref name="fieldExpression"/> 返回的 fieldName</param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> WhereField<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            var fields = fieldExpression.GetMemberNames();
            fields.ForEach(field => WhereField(field, operation, keyword, include, paramName));
            return this;
        }

        public virtual SqlFactory<TEntity> GroupByField<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return GroupByField(fieldExpression.GetMemberNames().ToArray());
        }

        public virtual SqlFactory<TEntity> OrderByField<TProperty>(OrderByType type, Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return OrderByField(type, fieldExpression.GetMemberNames().ToArray());
        }

        public virtual SqlFactory<TEntity> BulkInsert(IEnumerable<TEntity> entities)
        {
            if (entities != null)
            {
                _bulkInsertEntityCount = entities.Count();

                var paramDic = new Dictionary<string, object>();
                var index = 0;
                foreach (var entity in entities)
                {
                    index++;
                    foreach (var fieldInfo in typeof(TEntity).GetEntityInfo().FieldInfos)
                    {
                        paramDic.Add($"{fieldInfo.FieldName}{index}", fieldInfo.Property.GetValue(entity, null));
                    }
                }

                SetParameter(paramDic);
            }
            return this;
        }
    }
}
