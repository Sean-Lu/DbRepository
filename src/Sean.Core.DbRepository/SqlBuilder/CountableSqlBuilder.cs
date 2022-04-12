using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public abstract class CountableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "SELECT COUNT(1) FROM {0}{1};";

        protected CountableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
        }
    }

    public class CountableSqlBuilder<TEntity> : CountableSqlBuilder, ICountable<TEntity>
    {
        private string JoinTableSql => _joinTable.IsValueCreated && _joinTable.Value.Length > 0 ? _joinTable.Value.ToString() : string.Empty;
        private string WhereSql => _where.IsValueCreated && _where.Value.Length > 0 ? $" WHERE {_where.Value.ToString()}" : string.Empty;

        private readonly Lazy<StringBuilder> _joinTable = new();
        private readonly Lazy<StringBuilder> _where = new();

        private bool MultiTable => _joinTable.IsValueCreated && _joinTable.Value.Length > 0;

        private object _parameter;

        private CountableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
        }

        /// <summary>
        /// Create an instance of <see cref="ICountable{TEntity}"/>.
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static ICountable<TEntity> Create(DatabaseType dbType, string tableName = null)
        {
            var sqlBuilder = new CountableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
            return sqlBuilder;
        }

        #region [Join] 表关联
        public virtual ICountable<TEntity> InnerJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" INNER JOIN {joinTableSql}");
            }
            return this;
        }
        public virtual ICountable<TEntity> LeftJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" LEFT JOIN {joinTableSql}");
            }
            return this;
        }
        public virtual ICountable<TEntity> RightJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" RIGHT JOIN {joinTableSql}");
            }
            return this;
        }
        public virtual ICountable<TEntity> FullJoin(string joinTableSql)
        {
            if (!string.IsNullOrWhiteSpace(joinTableSql))
            {
                this._joinTable.Value.Append($" FULL JOIN {joinTableSql}");
            }
            return this;
        }

        public virtual ICountable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return InnerJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        public virtual ICountable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return LeftJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        public virtual ICountable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return RightJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        public virtual ICountable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2)
        {
            var joinTableName = typeof(TEntity2).GetMainTableName();
            return FullJoin($"{SqlAdapter.FormatTableName(joinTableName)} ON {SqlBuilderUtil.GetJoinFields(SqlAdapter, fieldExpression, fieldExpression2, joinTableName)}");
        }
        #endregion

        #region [WHERE]
        public virtual ICountable<TEntity> Where(string where)
        {
            SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.None, where);
            return this;
        }
        public virtual ICountable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression)
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
        public virtual ICountable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

        public virtual ICountable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            if (MultiTable)
            {
                SqlAdapter.MultiTable = true;
            }
            SqlBuilderUtil.WhereField(SqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
            return this;
        }
        public virtual ICountable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            var aqlAdapter = new DefaultSqlAdapter(SqlAdapter.DbType, typeof(TEntity2).GetMainTableName())
            {
                MultiTable = true
            };
            SqlBuilderUtil.WhereField(aqlAdapter, _where.Value, fieldExpression, operation, keyword, include, paramName);
            return this;
        }

        public virtual ICountable<TEntity> AndWhere(string where)
        {
            SqlBuilderUtil.Where(_where.Value, WhereSqlKeyword.And, where);
            return this;
        }
        public virtual ICountable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression)
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
        public virtual ICountable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression)
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

        public virtual ICountable<TEntity> SetParameter(object param)
        {
            _parameter = param;
            return this;
        }

        public virtual ICountableSql Build()
        {
            var sb = new StringBuilder();
            sb.Append(string.Format(SqlTemplate, $"{SqlAdapter.FormatTableName()}{JoinTableSql}", WhereSql));

            var countableSql = new DefaultCountableSql
            {
                Sql = sb.ToString(),
                Parameter = _parameter
            };
            return countableSql;
        }
    }

    public interface ICountable
    {
        ISqlAdapter SqlAdapter { get; }

        /// <summary>
        /// 创建统计数量的SQL：<see cref="CountableSqlBuilder.SqlTemplate"/>
        /// </summary>
        /// <returns></returns>
        ICountableSql Build();
    }

    public interface ICountable<TEntity> : ICountable
    {
        #region [Join] 表关联
        /// <summary>
        /// INNER JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：INNER JOIN</param>
        /// <returns></returns>
        ICountable<TEntity> InnerJoin(string joinTableSql);
        /// <summary>
        /// LEFT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：LEFT JOIN</param>
        /// <returns></returns>
        ICountable<TEntity> LeftJoin(string joinTableSql);
        /// <summary>
        /// RIGHT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：RIGHT JOIN</param>
        /// <returns></returns>
        ICountable<TEntity> RightJoin(string joinTableSql);
        /// <summary>
        /// FULL JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：FULL JOIN</param>
        /// <returns></returns>
        ICountable<TEntity> FullJoin(string joinTableSql);

        /// <summary>
        /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        ICountable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        ICountable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        ICountable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        ICountable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        #endregion

        #region [WHERE]
        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="where">不包含关键字：WHERE</param>
        /// <returns></returns>
        ICountable<TEntity> Where(string where);
        /// <summary>
        /// 解析WHERE过滤条件
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        ICountable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
        ICountable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="operation"></param>
        /// <param name="keyword"></param>
        /// <param name="include"></param>
        /// <param name="paramName">参数名称，默认同 <paramref name="fieldName"/></param>
        /// <returns></returns>
        ICountable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
        ICountable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

        ICountable<TEntity> AndWhere(string where);
        ICountable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
        ICountable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
        #endregion

        /// <summary>
        /// 设置SQL入参
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        ICountable<TEntity> SetParameter(object param);
    }
}