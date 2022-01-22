using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Contracts;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Factory
{
    public class SqlFactory
    {
        #region SQL
        /// <summary>
        /// SQL：新增数据
        /// </summary>
        public virtual string InsertSql
        {
            get
            {
                var list = IncludeFieldsList.Except(IdentityFieldsList).ToList();
                if (!list.Any())
                    return string.Empty;
                var fields = list.Select(fieldName => DbType.MarkAsTableOrFieldName(fieldName));
                var parameters = list.Select(fieldName => DbType.MarkAsInputParameter(fieldName));
                return $"INSERT INTO {DbType.MarkAsTableOrFieldName(TableName)}({string.Join(", ", fields)}) VALUES({string.Join(", ", parameters)});{(_returnLastInsertId ? DbType.GetSqlForSelectLastInsertId() : string.Empty)}";
            }
        }
        /// <summary>
        /// SQL：删除数据（为了防止误删除，需要指定WHERE过滤条件，否则会主动抛出异常）
        /// </summary>
        public virtual string DeleteSql
        {
            get
            {
                if (string.IsNullOrWhiteSpace(WhereSql))
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

                return $"DELETE FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql};";
            }
        }
        /// <summary>
        /// SQL：删除表所有数据（忽略WHERE过滤条件）
        /// </summary>
        public virtual string DeleteAllSql
        {
            get
            {
                return $"DELETE FROM {DbType.MarkAsTableOrFieldName(TableName)};";
            }
        }
        /// <summary>
        /// SQL：更新数据（为了防止误更新，需要指定WHERE过滤条件，否则会主动抛出异常）
        /// </summary>
        public virtual string UpdateSql
        {
            get
            {
                if (string.IsNullOrWhiteSpace(WhereSql))
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(WhereSql));

                var list = IncludeFieldsList.Except(IdentityFieldsList).ToList();
                if (!list.Any())
                    return string.Empty;
                var sets = list.Select(fieldName => $"{DbType.MarkAsTableOrFieldName(fieldName)}={DbType.MarkAsInputParameter(fieldName)}");
                return $"UPDATE {DbType.MarkAsTableOrFieldName(TableName)} SET {string.Join(", ", sets)}{WhereSql};";
            }
        }
        /// <summary>
        /// SQL：更新表所有数据（忽略WHERE过滤条件）
        /// </summary>
        public virtual string UpdateAllSql
        {
            get
            {
                var list = IncludeFieldsList.Except(IdentityFieldsList).ToList();
                if (!list.Any())
                    return string.Empty;
                var sets = list.Select(fieldName => $"{DbType.MarkAsTableOrFieldName(fieldName)}={DbType.MarkAsInputParameter(fieldName)}");
                return $"UPDATE {DbType.MarkAsTableOrFieldName(TableName)} SET {string.Join(", ", sets)};";
            }
        }
        /// <summary>
        /// SQL：查询数据
        /// </summary>
        public virtual string QuerySql
        {
            get
            {
                var selectFields = IncludeFieldsList.Any() ? string.Join(", ", IncludeFieldsList.Select(fieldName => $"{DbType.MarkAsTableOrFieldName(fieldName)}")) : "*";
                //const string rowNumAlias = "ROW_NUM";
                if (TopNumber != 0)
                {
                    // 查询前几行
                    switch (DbType)
                    {
                        case DatabaseType.MySql:
                        case DatabaseType.SQLite:
                        case DatabaseType.PostgreSql:
                            return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {TopNumber};";
                        case DatabaseType.SqlServer:
                        case DatabaseType.SqlServerCe:
                        case DatabaseType.Access:
                            return $"SELECT TOP {TopNumber} {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                        case DatabaseType.Oracle:
                            var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {TopNumber}" : $"{WhereSql} AND ROWNUM <= {TopNumber}";
                            return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{sqlWhere}{GroupBySql}{HavingSql}{OrderBySql};";
                        default:
                            //throw new NotSupportedException($"[{nameof(QuerySql)}]-[{_dbType}]-[{nameof(TopNumber)}:{TopNumber}]");
                            return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {TopNumber};";// 同MySql
                    }
                }
                else if (PageIndex >= 1)
                {
                    // 分页查询
                    switch (DbType)
                    {
                        case DatabaseType.MySql:
                        case DatabaseType.SQLite:
                            return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {(PageIndex - 1) * PageSize},{PageSize};";
                        case DatabaseType.PostgreSql:
                            return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {PageSize} OFFSET {(PageIndex - 1) * PageSize};";
                        case DatabaseType.SqlServer:
                        case DatabaseType.SqlServerCe:
                            return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} OFFSET {(PageIndex - 1) * PageSize} ROWS FETCH NEXT {PageSize} ROWS ONLY;";
                        case DatabaseType.DB2:
                            // SQL Server、Oracle等数据库都支持：ROW_NUMBER() OVER()
                            return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {(PageIndex - 1) * PageSize} AND t2.ROW_NUM <= {(PageIndex - 1) * PageSize + PageSize};";
                        case DatabaseType.Access:
                            return $"SELECT TOP {PageSize} {selectFields} FROM (SELECT TOP {(PageIndex - 1) * PageSize + PageSize} {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}) t2;";
                        case DatabaseType.Oracle:
                            if (string.IsNullOrWhiteSpace(OrderBySql))
                            {
                                // 无ORDER BY排序
                                // SQL示例：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 AND ROWNUM<=10) t2 WHERE t2.ROW_NUM>5;
                                var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {(PageIndex - 1) * PageSize + PageSize}" : $"{WhereSql} AND ROWNUM <= {(PageIndex - 1) * PageSize + PageSize}";
                                return $"SELECT {selectFields} FROM (SELECT ROWNUM ROW_NUM, {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{sqlWhere}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {(PageIndex - 1) * PageSize};";
                            }
                            else
                            {
                                // 有ORDER BY排序
                                // SQL示例1：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM (SELECT ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 ORDER BY ID DESC) t2 WHERE ROWNUM<=10) t3 WHERE t3.ROW_NUM>5
                                // SQL示例2：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROW_NUMBER() OVER(ORDER BY ID DESC) ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456) t2 WHERE t2.ROW_NUM>5 AND t2.ROW_NUM<=10;
                                return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {(PageIndex - 1) * PageSize} AND t2.ROW_NUM <= {(PageIndex - 1) * PageSize + PageSize};";
                            }
                        default:
                            //throw new NotSupportedException($"[{nameof(QuerySql)}]-[{_dbType}]-[{nameof(PageIndex)}:{PageIndex},{nameof(PageSize)}:{PageSize}]");
                            return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {(PageIndex - 1) * PageSize},{PageSize};";// 同MySql
                    }
                }
                else
                {
                    // 普通查询
                    return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                }
            }
        }
        /// <summary>
        /// SQL：查询数据（忽略WHERE过滤条件）
        /// </summary>
        public virtual string QueryAllSql
        {
            get
            {
                var selectFields = IncludeFieldsList.Any() ? string.Join(", ", IncludeFieldsList.Select(fieldName => $"{DbType.MarkAsTableOrFieldName(fieldName)}")) : "*";
                return $"SELECT {selectFields} FROM {DbType.MarkAsTableOrFieldName(TableName)};";
            }
        }
        /// <summary>
        /// SQL：统计数量
        /// </summary>
        public virtual string CountSql
        {
            get
            {
                return $"SELECT COUNT(1) FROM {DbType.MarkAsTableOrFieldName(TableName)}{WhereSql}{GroupBySql}{HavingSql};";
            }
        }
        /// <summary>
        /// SQL：统计数量（忽略WHERE过滤条件）
        /// </summary>
        public virtual string CountAllSql
        {
            get
            {
                return $"SELECT COUNT(1) FROM {DbType.MarkAsTableOrFieldName(TableName)};";
            }
        }

        public string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;
        public string GroupBySql => _groupBy.IsValueCreated && _groupBy.Value.Length > 0 ? $" GROUP BY {_groupBy.Value.ToString()}" : string.Empty;
        public string HavingSql => _having.IsValueCreated && _having.Value.Length > 0 ? $" HAVING {_having.Value.ToString()}" : string.Empty;
        public string OrderBySql => _orderBy.IsValueCreated && _orderBy.Value.Length > 0 ? $" ORDER BY {_orderBy.Value.ToString()}" : string.Empty;
        #endregion

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DbType { get; }

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
        public List<string> IncludeFieldsList { get; } = new();
        /// <summary>
        /// 忽略字段
        /// </summary>
        public List<string> IgnoreFieldsList { get; } = new();
        /// <summary>
        /// 自增字段
        /// </summary>
        public List<string> IdentityFieldsList { get; } = new();
        /// <summary>
        /// SELECT TOP {<see cref="TopNumber"/>}
        /// </summary>
        public int TopNumber { get; private set; }
        /// <summary>
        /// LIMIT {(<see cref="PageIndex"/> - 1) * <see cref="PageSize"/>},{<see cref="PageSize"/>}
        /// </summary>
        public int PageIndex { get; private set; }
        /// <summary>
        /// LIMIT {(<see cref="PageIndex"/> - 1) * <see cref="PageSize"/>},{<see cref="PageSize"/>}
        /// </summary>
        public int PageSize { get; private set; }

        private readonly Lazy<StringBuilder> _where = new();
        private readonly Lazy<StringBuilder> _groupBy = new();
        private readonly Lazy<StringBuilder> _having = new();
        private readonly Lazy<StringBuilder> _orderBy = new();
        private bool _returnLastInsertId;

        protected SqlFactory(DatabaseType dbType, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            DbType = dbType;
            TableName = tableName;
        }

        public static SqlFactory Build(DatabaseType dbType, string tableName)
        {
            return new(dbType, tableName);
        }

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
                    if (!string.IsNullOrWhiteSpace(field) && !IncludeFieldsList.Contains(field))
                    {
                        IncludeFieldsList.Add(field);
                        if (IgnoreFieldsList.Contains(field))
                        {
                            IgnoreFieldsList.Remove(field);
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
                    if (!string.IsNullOrWhiteSpace(field) && !IgnoreFieldsList.Contains(field))
                    {
                        IgnoreFieldsList.Add(field);
                        if (IncludeFieldsList.Contains(field))
                        {
                            IncludeFieldsList.Remove(field);
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
                    if (!string.IsNullOrWhiteSpace(field) && !IdentityFieldsList.Contains(field))
                    {
                        IdentityFieldsList.Add(field);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// 查询前几行（仅用于 <see cref="QuerySql"/> ）
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public virtual SqlFactory Top(int top)
        {
            this.TopNumber = top;
            return this;
        }
        /// <summary>
        /// 分页查询条件（仅用于 <see cref="QuerySql"/> ）
        /// </summary>
        /// <param name="pageIndex">最小值为1</param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public virtual SqlFactory Page(int pageIndex, int pageSize)
        {
            this.PageIndex = Math.Max(1, pageIndex);
            this.PageSize = pageSize;
            return this;
        }

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
        public virtual SqlFactory WhereField(string fieldName, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None)
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
                this._where.Value.Append($"{DbType.MarkAsTableOrFieldName(fieldName)} {operation.ToSqlString()} {DbType.MarkAsInputParameter(fieldName)}");
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
                GroupBy(string.Join(", ", fieldNames));
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
        public virtual SqlFactory OrderByField(OrderByType type, params string[] fieldNames)
        {
            if (fieldNames != null && fieldNames.Any())
            {
                if (_orderBy.Value.Length > 0) this._orderBy.Value.Append(", ");

                _orderBy.Value.Append($"{string.Join(", ", fieldNames)} {type.ToSqlString()}");
            }
            return this;
        }

        /// <summary>
        /// 仅用于 <see cref="InsertSql"/>
        /// </summary>
        /// <param name="returnLastInsertId"></param>
        /// <returns></returns>
        public virtual SqlFactory ReturnLastInsertId(bool returnLastInsertId)
        {
            this._returnLastInsertId = returnLastInsertId;
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
    }

    public class SqlFactory<TEntity> : SqlFactory
    {
        #region override properties
        /// <summary>
        /// SQL：删除数据（如果没有指定WHERE过滤条件，则过滤条件默认使用 <see cref="KeyAttribute"/> 主键字段）
        /// </summary>
        public override string DeleteSql
        {
            get
            {
                if (string.IsNullOrWhiteSpace(WhereSql))
                {
                    typeof(TEntity).GetPrimaryKeys().ForEach(fieldName => WhereField(fieldName, SqlOperation.Equal));
                }

                return base.DeleteSql;
            }
        }

        /// <summary>
        /// SQL：更新数据（如果没有指定WHERE过滤条件，则过滤条件默认使用 <see cref="KeyAttribute"/> 主键字段）
        /// </summary>
        public override string UpdateSql
        {
            get
            {
                if (string.IsNullOrWhiteSpace(WhereSql))
                {
                    typeof(TEntity).GetPrimaryKeys().ForEach(fieldName => WhereField(fieldName, SqlOperation.Equal));
                }

                return base.UpdateSql;
            }
        }
        #endregion

        private SqlFactory(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {

        }

        /// <summary>
        /// 创建新的 <see cref="SqlFactory{TEntity}"/> 实例
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="tableName"></param>
        /// <param name="autoIncludeFields">
        /// 是否自动包含字段（通过反射机制实现）：
        /// <para><see cref="SqlFactory.IncludeFields"/></para>
        /// <para><see cref="SqlFactory.IdentityFields"/></para>
        /// </param>
        /// <returns></returns>
        public static SqlFactory<TEntity> Build(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            var sqlFactory = new SqlFactory<TEntity>(dbType, !string.IsNullOrWhiteSpace(tableName) ? tableName : typeof(TEntity).GetMainTableName());
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
        /// <param name="autoIncludeFields">
        /// 是否自动包含字段（通过反射机制实现）：
        /// <para><see cref="SqlFactory.IncludeFields"/></para>
        /// <para><see cref="SqlFactory.IdentityFields"/></para>
        /// </param>
        /// <returns></returns>
        public static SqlFactory<TEntity> Build(IBaseRepository repository, bool autoIncludeFields)
        {
            return Build(repository.Factory.DbType, autoIncludeFields, repository.TableName());
        }

        #region override methods
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
        /// 查询前几行（仅用于 <see cref="SqlFactory.QuerySql"/> ）
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Top(int top)
        {
            return base.Top(top) as SqlFactory<TEntity>;
        }
        /// <summary>
        /// 分页查询条件（仅用于 <see cref="SqlFactory.QuerySql"/> ）
        /// </summary>
        /// <param name="pageIndex">最小值为1</param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Page(int pageIndex, int pageSize)
        {
            return base.Page(pageIndex, pageSize) as SqlFactory<TEntity>;
        }

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="where">不包含关键字：WHERE</param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> Where(string where)
        {
            return base.Where(where) as SqlFactory<TEntity>;
        }
        public new virtual SqlFactory<TEntity> WhereField(string fieldName, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None)
        {
            return base.WhereField(fieldName, operation, keyword, include) as SqlFactory<TEntity>;
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
        public new virtual SqlFactory<TEntity> OrderByField(OrderByType type, params string[] fieldNames)
        {
            return base.OrderByField(type, fieldNames) as SqlFactory<TEntity>;
        }

        /// <summary>
        /// 仅用于 <see cref="SqlFactory.InsertSql"/>
        /// </summary>
        /// <param name="returnLastInsertId"></param>
        /// <returns></returns>
        public new virtual SqlFactory<TEntity> ReturnLastInsertId(bool returnLastInsertId)
        {
            return base.ReturnLastInsertId(returnLastInsertId) as SqlFactory<TEntity>;
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
        #endregion

        public virtual SqlFactory<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return IncludeFields(fieldExpression.GetMemberName());
        }
        public virtual SqlFactory<TEntity> IncludeFields(params Expression<Func<TEntity, object>>[] fieldExpression)
        {
            return IncludeFields(fieldExpression.Select(c => c.GetMemberName()).ToArray());
        }
        public virtual SqlFactory<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return IgnoreFields(fieldExpression.GetMemberName());
        }
        public virtual SqlFactory<TEntity> IgnoreFields(params Expression<Func<TEntity, object>>[] fieldExpression)
        {
            return IgnoreFields(fieldExpression.Select(c => c.GetMemberName()).ToArray());
        }
        public virtual SqlFactory<TEntity> IdentityFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return IdentityFields(fieldExpression.GetMemberName());
        }
        public virtual SqlFactory<TEntity> IdentityFields(params Expression<Func<TEntity, object>>[] fieldExpression)
        {
            return IdentityFields(fieldExpression.Select(c => c.GetMemberName()).ToArray());
        }

        public virtual SqlFactory<TEntity> WhereField<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None)
        {
            return WhereField(fieldExpression.GetMemberName(), operation, keyword, include);
        }

        public virtual SqlFactory<TEntity> GroupByField<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return GroupByField(fieldExpression.GetMemberName());
        }
        public virtual SqlFactory<TEntity> GroupByField(params Expression<Func<TEntity, object>>[] fieldExpression)
        {
            return GroupByField(fieldExpression.Select(c => c.GetMemberName()).ToArray());
        }

        public virtual SqlFactory<TEntity> OrderByField<TProperty>(OrderByType type, Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return OrderByField(type, fieldExpression.GetMemberName());
        }
        public virtual SqlFactory<TEntity> OrderByField(OrderByType type, params Expression<Func<TEntity, object>>[] fieldExpression)
        {
            return OrderByField(type, fieldExpression.Select(c => c.GetMemberName()).ToArray());
        }

        /// <summary>
        /// 仅用于 <see cref="InsertSql"/>
        /// </summary>
        /// <param name="returnLastInsertId"></param>
        /// <param name="keyIdentityProperty"></param>
        /// <returns></returns>
        public virtual SqlFactory<TEntity> ReturnLastInsertId(bool returnLastInsertId, out PropertyInfo keyIdentityProperty)
        {
            base.ReturnLastInsertId(returnLastInsertId);

            keyIdentityProperty = null;
            if (returnLastInsertId)
            {
                keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty();
            }
            return this;
        }
    }
}
