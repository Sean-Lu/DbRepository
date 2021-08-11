using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Contracts;
using Sean.Core.DbRepository.Extensions;
#if !NET40
using System.ComponentModel.DataAnnotations;
#endif

namespace Sean.Core.DbRepository.Factory
{
    public class SqlFactory
    {
        protected SqlFactory(DatabaseType dbType, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            _dbType = dbType;
            _tableName = tableName;

            _includeFields = new List<string>();
            _ignoreFields = new List<string>();
            _identityFields = new List<string>();
        }

        public static SqlFactory Build(DatabaseType dbType, string tableName)
        {
            return new(dbType, tableName);
        }

#region SQL
        /// <summary>
        /// SQL：新增数据
        /// </summary>
        public virtual string InsertSql
        {
            get
            {
                var list = _includeFields.Except(_identityFields).ToList();
                if (!list.Any())
                    return string.Empty;
                var fields = list.Select(c => _dbType.MarkAsTableOrFieldName(c));
                var parameters = list.Select(c => _dbType.MarkAsInputParameter(c));
                return $"INSERT INTO {_dbType.MarkAsTableOrFieldName(_tableName)}({string.Join(", ", fields)}) VALUES({string.Join(", ", parameters)});{(_returnLastInsertId ? _dbType.GetSqlForSelectLastInsertId() : string.Empty)}";
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

                return $"{DeleteAllSql}{WhereSql};";
            }
        }
        /// <summary>
        /// SQL：删除表所有数据（忽略WHERE过滤条件）
        /// </summary>
        public virtual string DeleteAllSql
        {
            get
            {
                return $"DELETE FROM {_dbType.MarkAsTableOrFieldName(_tableName)}";
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

                return $"{UpdateAllSql}{WhereSql};";
            }
        }
        /// <summary>
        /// SQL：更新表所有数据（忽略WHERE过滤条件）
        /// </summary>
        public virtual string UpdateAllSql
        {
            get
            {
                var list = _includeFields.Except(_identityFields).ToList();
                if (!list.Any())
                    return string.Empty;
                var sets = list.Select(c => $"{_dbType.MarkAsTableOrFieldName(c)}={_dbType.MarkAsInputParameter(c)}");
                return $"UPDATE {_dbType.MarkAsTableOrFieldName(_tableName)} SET {string.Join(", ", sets)}{WhereSql}";
            }
        }
        /// <summary>
        /// SQL：查询数据
        /// </summary>
        public virtual string QuerySql
        {
            get
            {
                var selectFields = _includeFields.Any() ? string.Join(", ", _includeFields.Select(c => $"{_dbType.MarkAsTableOrFieldName(c)}")) : "*";
                if (_topNum != 0)
                {
                    switch (_dbType)
                    {
                        case DatabaseType.MySql:
                        case DatabaseType.SQLite:
                        case DatabaseType.PostgreSql:
                            return $"SELECT {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_topNum};";
                        case DatabaseType.SqlServer:
                        case DatabaseType.SqlServerCe:
                        case DatabaseType.Access:
                            return $"SELECT TOP {_topNum} {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                        case DatabaseType.Oracle:
                            var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {_topNum}" : $"{WhereSql} AND ROWNUM <= {_topNum}";
                            return $"SELECT {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{sqlWhere}{GroupBySql}{HavingSql}{OrderBySql};";
                        default:
                            throw new NotSupportedException($"[{nameof(QuerySql)}]-[{_dbType}]-[{nameof(_topNum)}:{_topNum}]");
                    }
                }
                else
                {
                    return $"SELECT {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                }
            }
        }
        /// <summary>
        /// SQL：查询分页数据
        /// </summary>
        public virtual string QueryPageSql
        {
            get
            {
                var selectFields = _includeFields.Any() ? string.Join(", ", _includeFields.Select(c => $"{_dbType.MarkAsTableOrFieldName(c)}")) : "*";
                //const string rowNumAlias = "ROW_NUM";
                if (_pageIndex >= 1)
                {
                    switch (_dbType)
                    {
                        case DatabaseType.MySql:
                        case DatabaseType.SQLite:
                            return $"SELECT {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {(_pageIndex - 1) * _pageSize},{_pageSize};";
                        case DatabaseType.PostgreSql:
                            return $"SELECT {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} LIMIT {_pageSize} OFFSET {(_pageIndex - 1) * _pageSize};";
                        case DatabaseType.SqlServer:
                        case DatabaseType.SqlServerCe:
                            return $"SELECT {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql} OFFSET {(_pageIndex - 1) * _pageSize} ROWS FETCH NEXT {_pageSize} ROWS ONLY;";
                        case DatabaseType.DB2:
                            // SQL Server、Oracle等数据库都支持：ROW_NUMBER() OVER()
                            return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {(_pageIndex - 1) * _pageSize} AND t2.ROW_NUM <= {(_pageIndex - 1) * _pageSize + _pageSize};";
                        case DatabaseType.Access:
                            return $"SELECT TOP {_pageSize} {selectFields} FROM (SELECT TOP {(_pageIndex - 1) * _pageSize + _pageSize} {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql}) t2;";
                        case DatabaseType.Oracle:
                            if (string.IsNullOrWhiteSpace(OrderBySql))
                            {
                                // 无ORDER BY排序
                                // SQL示例：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 AND ROWNUM<=10) t2 WHERE t2.ROW_NUM>5;
                                var sqlWhere = string.IsNullOrEmpty(WhereSql) ? $" WHERE ROWNUM <= {(_pageIndex - 1) * _pageSize + _pageSize}" : $"{WhereSql} AND ROWNUM <= {(_pageIndex - 1) * _pageSize + _pageSize}";
                                return $"SELECT {selectFields} FROM (SELECT ROWNUM ROW_NUM, {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{sqlWhere}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {(_pageIndex - 1) * _pageSize};";
                            }
                            else
                            {
                                // 有ORDER BY排序
                                // SQL示例1：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROWNUM ROW_NUM, ID, SITE_ID FROM (SELECT ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456 ORDER BY ID DESC) t2 WHERE ROWNUM<=10) t3 WHERE t3.ROW_NUM>5
                                // SQL示例2：SELECT ROW_NUM, ID, SITE_ID FROM (SELECT ROW_NUMBER() OVER(ORDER BY ID DESC) ROW_NUM, ID, SITE_ID FROM SITE_TEST WHERE SITE_ID=123456) t2 WHERE t2.ROW_NUM>5 AND t2.ROW_NUM<=10;
                                return $"SELECT {selectFields} FROM (SELECT ROW_NUMBER() OVER({OrderBySql}) ROW_NUM, {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}) t2 WHERE t2.ROW_NUM > {(_pageIndex - 1) * _pageSize} AND t2.ROW_NUM <= {(_pageIndex - 1) * _pageSize + _pageSize};";
                            }
                        default:
                            throw new NotSupportedException($"[{nameof(QueryPageSql)}]-[{_dbType}]-[{nameof(_pageIndex)}:{_pageIndex},{nameof(_pageSize)}:{_pageSize}]");
                    }
                }
                else
                {
                    return $"SELECT {selectFields} FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql}{OrderBySql};";
                }
            }
        }
        /// <summary>
        /// SQL：统计数量
        /// </summary>
        public virtual string CountSql
        {
            get
            {
                return $"SELECT COUNT(1) FROM {_dbType.MarkAsTableOrFieldName(_tableName)}{WhereSql}{GroupBySql}{HavingSql};";
            }
        }

        public string WhereSql => !string.IsNullOrWhiteSpace(_where) ? $" WHERE {_where}" : string.Empty;
        public string GroupBySql => !string.IsNullOrWhiteSpace(_groupBy) ? $" GROUP BY {_groupBy}" : string.Empty;
        public string HavingSql => !string.IsNullOrWhiteSpace(_having) ? $" HAVING {_having}" : string.Empty;
        public string OrderBySql => !string.IsNullOrWhiteSpace(_orderBy) ? $" ORDER BY {_orderBy}" : string.Empty;
#endregion

        protected readonly DatabaseType _dbType;
        protected readonly string _tableName;
        protected bool _returnLastInsertId;
        /// <summary>
        /// 包含字段
        /// </summary>
        protected List<string> _includeFields;
        /// <summary>
        /// 忽略字段
        /// </summary>
        protected List<string> _ignoreFields;
        /// <summary>
        /// 自增字段
        /// </summary>
        protected List<string> _identityFields;
        protected int _topNum;
        protected int _pageIndex;
        protected int _pageSize;
        protected string _where;
        private string _groupBy;
        private string _having;
        private string _orderBy;

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
                    if (!string.IsNullOrWhiteSpace(field) && !_includeFields.Contains(field))
                    {
                        _includeFields.Add(field);
                        if (_ignoreFields.Contains(field))
                        {
                            _ignoreFields.Remove(field);
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
                    if (!string.IsNullOrWhiteSpace(field) && !_ignoreFields.Contains(field))
                    {
                        _ignoreFields.Add(field);
                        if (_includeFields.Contains(field))
                        {
                            _includeFields.Remove(field);
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
                    if (!string.IsNullOrWhiteSpace(field) && !_identityFields.Contains(field))
                    {
                        _identityFields.Add(field);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// TOP：仅用于 <see cref="QuerySql"/>
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public virtual SqlFactory Top(int top)
        {
            this._topNum = top;
            return this;
        }
        /// <summary>
        /// 分页条件：仅用于 <see cref="QueryPageSql"/>
        /// </summary>
        /// <param name="pageIndex">最小值为1</param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public virtual SqlFactory Page(int pageIndex, int pageSize)
        {
            this._pageIndex = Math.Max(1, pageIndex);
            this._pageSize = pageSize;
            return this;
        }

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="where">不包含关键字：WHERE</param>
        /// <returns></returns>
        public virtual SqlFactory Where(string where)
        {
            this._where = where;
            return this;
        }
        /// <summary>
        /// GROUP BY column_name
        /// </summary>
        /// <param name="groupBy">不包含关键字：GROUP BY</param>
        /// <returns></returns>
        public virtual SqlFactory GroupBy(string groupBy)
        {
            this._groupBy = groupBy;
            return this;
        }
        /// <summary>
        /// HAVING aggregate_function(column_name) operator value
        /// </summary>
        /// <param name="having">不包含关键字：HAVING</param>
        /// <returns></returns>
        public virtual SqlFactory Having(string having)
        {
            this._having = having;
            return this;
        }
        /// <summary>
        /// ORDER BY column_name,column_name ASC|DESC;
        /// </summary>
        /// <param name="orderBy">不包含关键字：ORDER BY</param>
        /// <returns></returns>
        public virtual SqlFactory OrderBy(string orderBy)
        {
            this._orderBy = orderBy;
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
    }

#if !NET40
    public class SqlFactory<TEntity> : SqlFactory
    {
        /// <summary>
        /// SQL：删除数据（如果没有指定WHERE过滤条件，则过滤条件默认使用 <see cref="KeyAttribute"/> 主键字段）
        /// </summary>
        public override string DeleteSql
        {
            get
            {
                if (string.IsNullOrWhiteSpace(WhereSql))
                {
                    var keys = typeof(TEntity).GetPrimaryKeyProperties().Select(c => $"{_dbType.MarkAsTableOrFieldName(c.Name)}={_dbType.MarkAsInputParameter(c.Name)}");
                    return $"{DeleteAllSql} WHERE {string.Join(" AND ", keys)};";
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
                    var keys = typeof(TEntity).GetPrimaryKeyProperties().Select(c => $"{_dbType.MarkAsTableOrFieldName(c.Name)}={_dbType.MarkAsInputParameter(c.Name)}");
                    return $"{UpdateAllSql} WHERE {string.Join(" AND ", keys)};";
                }

                return base.UpdateSql;
            }
        }

        private SqlFactory(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {

        }

        /// <summary>
        /// 创建新的 <see cref="SqlFactory{TEntity}"/> 实例
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="tableName"></param>
        /// <param name="autoIncludeFields">是否自动包含字段（通过反射机制实现）：<see cref="SqlFactory.IncludeFields"/>、<see cref="SqlFactory.IdentityFields"/></param>
        /// <returns></returns>
        public static SqlFactory<TEntity> Build(DatabaseType dbType, string tableName = null, bool autoIncludeFields = true)
        {
            var sqlFactory = new SqlFactory<TEntity>(dbType, !string.IsNullOrWhiteSpace(tableName) ? tableName : typeof(TEntity).GetMainTableName());
            if (autoIncludeFields)
            {
                sqlFactory.IncludeFields(typeof(TEntity).GetValidPropertiesForSql().Select(c => c.Name).ToArray());
                sqlFactory.IdentityFields(typeof(TEntity).GetIdentityProperties().Select(c => c.Name).ToArray());
            }
            return sqlFactory;
        }
        /// <summary>
        /// 创建新的 <see cref="SqlFactory{TEntity}"/> 实例
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="autoIncludeFields">是否自动包含字段（通过反射机制实现）：<see cref="SqlFactory.IncludeFields"/>、<see cref="SqlFactory.IdentityFields"/></param>
        /// <returns></returns>
        public static SqlFactory<TEntity> Build(IBaseRepository repository, bool autoIncludeFields = true)
        {
            return Build(repository.Factory.DbType, repository.TableName(), autoIncludeFields);
        }

        /// <summary>
        /// 仅用于 <see cref="InsertSql"/>
        /// </summary>
        /// <param name="returnLastInsertId"></param>
        /// <param name="keyIdentityProperty"></param>
        /// <returns></returns>
        public SqlFactory<TEntity> ReturnLastInsertId(bool returnLastInsertId, out PropertyInfo keyIdentityProperty)
        {
            keyIdentityProperty = null;
            if (returnLastInsertId)
            {
                keyIdentityProperty = typeof(TEntity).GetKeyIdentityProperty();
            }
            _returnLastInsertId = returnLastInsertId;
            return this;
        }
    }
#endif
}
