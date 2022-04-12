using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public abstract class UpdateableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "UPDATE {0} SET {1}{2};";

        protected UpdateableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
        }
    }

    public class UpdateableSqlBuilder<TEntity> : UpdateableSqlBuilder, IUpdateable<TEntity>
    {
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
        /// <param name="dbType"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
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

        public virtual IUpdateable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression, TEntity entity = default)
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
        public virtual IUpdateable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IgnoreFields(fields);
        }
        public virtual IUpdateable<TEntity> PrimaryKeyFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return PrimaryKeyFields(fields);
        }

        public virtual IUpdateable<TEntity> IncrFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
        {
            SqlBuilderUtil.IncrFields(SqlAdapter, _includeFieldsList, fieldExpression, value);
            return this;
        }
        public virtual IUpdateable<TEntity> DecrFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
        {
            SqlBuilderUtil.DecrFields(SqlAdapter, _includeFieldsList, fieldExpression, value);
            return this;
        }
        #endregion

        #region [Join] 表关联
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
            var aqlAdapter = new DefaultSqlAdapter(SqlAdapter.DbType, typeof(TEntity2).GetMainTableName())
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

        public virtual IUpdateableSql Build()
        {
            var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            if (!_allowEmptyWhereClause && string.IsNullOrWhiteSpace(WhereSql))
            {
                #region 设置默认过滤条件为主键字段
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
            sb.Append(string.Format(SqlTemplate, $"{SqlAdapter.FormatTableName()}{JoinTableSql}", string.Join(", ", sets), WhereSql));

            var updateableSql = new DefaultUpdateableSql
            {
                Sql = sb.ToString(),
                Parameter = _parameter
            };
            return updateableSql;
        }
    }

    public interface IUpdateable
    {
        ISqlAdapter SqlAdapter { get; }

        /// <summary>
        /// 创建更新数据的SQL：<see cref="UpdateableSqlBuilder.SqlTemplate"/>
        /// <para>1. 为了防止误更新，需要指定WHERE过滤条件，否则会抛出异常，可以通过 <see cref="IUpdateable{TEntity}.AllowEmptyWhereClause"/> 设置允许空 WHERE 子句</para>
        /// <para>2. 如果没有指定WHERE过滤条件，且没有设置 <see cref="IUpdateable{TEntity}.AllowEmptyWhereClause"/> 为true，则过滤条件默认使用 <see cref="KeyAttribute"/> 主键字段</para>
        /// </summary>
        /// <returns></returns>
        IUpdateableSql Build();
    }

    public interface IUpdateable<TEntity> : IUpdateable
    {
        #region [Field]
        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IUpdateable<TEntity> IncludeFields(params string[] fields);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IUpdateable<TEntity> IgnoreFields(params string[] fields);
        /// <summary>
        /// 主键字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IUpdateable<TEntity> PrimaryKeyFields(params string[] fields);

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        IUpdateable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression, TEntity entity = default);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IUpdateable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
        /// <summary>
        /// 主键字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IUpdateable<TEntity> PrimaryKeyFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);

        /// <summary>
        /// 递增字段
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IUpdateable<TEntity> IncrFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct;
        /// <summary>
        /// 递减字段
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IUpdateable<TEntity> DecrFields<TValue>(Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct;
        #endregion

        #region [Join] 表关联
        /// <summary>
        /// INNER JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：INNER JOIN</param>
        /// <returns></returns>
        IUpdateable<TEntity> InnerJoin(string joinTableSql);
        /// <summary>
        /// LEFT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：LEFT JOIN</param>
        /// <returns></returns>
        IUpdateable<TEntity> LeftJoin(string joinTableSql);
        /// <summary>
        /// RIGHT JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：RIGHT JOIN</param>
        /// <returns></returns>
        IUpdateable<TEntity> RightJoin(string joinTableSql);
        /// <summary>
        /// FULL JOIN
        /// </summary>
        /// <param name="joinTableSql">不包含关键字：FULL JOIN</param>
        /// <returns></returns>
        IUpdateable<TEntity> FullJoin(string joinTableSql);

        /// <summary>
        /// INNER JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IUpdateable<TEntity> InnerJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// LEFT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IUpdateable<TEntity> LeftJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// RIGHT JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IUpdateable<TEntity> RightJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        /// <summary>
        /// FULL JOIN table_name2 ON table_name1.column_name=table_name2.column_name
        /// </summary>
        /// <param name="fieldExpression"></param>
        /// <param name="fieldExpression2"></param>
        /// <returns></returns>
        IUpdateable<TEntity> FullJoin<TEntity2>(Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2);
        #endregion

        #region [WHERE]
        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="where">不包含关键字：WHERE</param>
        /// <returns></returns>
        IUpdateable<TEntity> Where(string where);
        /// <summary>
        /// 解析WHERE过滤条件
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        IUpdateable<TEntity> Where(Expression<Func<TEntity, bool>> whereExpression);
        IUpdateable<TEntity> Where<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);

        /// <summary>
        /// WHERE column_name operator value
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="operation"></param>
        /// <param name="keyword"></param>
        /// <param name="include"></param>
        /// <param name="paramName">参数名称，默认同 <paramref name="fieldName"/></param>
        /// <returns></returns>
        IUpdateable<TEntity> WhereField(Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);
        IUpdateable<TEntity> WhereField<TEntity2>(Expression<Func<TEntity2, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null);

        IUpdateable<TEntity> AndWhere(string where);
        IUpdateable<TEntity> AndWhere(Expression<Func<TEntity, bool>> whereExpression);
        IUpdateable<TEntity> AndWhere<TEntity2>(Expression<Func<TEntity2, bool>> whereExpression);
        #endregion

        /// <summary>
        /// 是否允许空的 WHERE 子句
        /// <para>注：为了防止执行错误的SQL导致不可逆的结果，默认不允许空的WHERE子句</para>
        /// </summary>
        /// <param name="allowEmptyWhereClause"></param>
        /// <returns></returns>
        IUpdateable<TEntity> AllowEmptyWhereClause(bool allowEmptyWhereClause = true);

        /// <summary>
        /// 设置SQL入参
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        IUpdateable<TEntity> SetParameter(object param);
    }
}