using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public abstract class ReplaceableSqlBuilder : BaseSqlBuilder
    {
        /// <summary>
        /// 支持该语法的数据库：
        /// <para>- <see cref="DatabaseType.MySql"/></para>
        /// <para>- <see cref="DatabaseType.SQLite"/></para>
        /// <para>注意：除非表有一个 PRIMARY KEY 或 UNIQUE 索引，否则使用一个 REPLACE 语句没有意义。</para>
        /// </summary>
        public const string SqlTemplate = "REPLACE INTO {0}({1}) VALUES{2};";

        protected ReplaceableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
        }
    }

    public class ReplaceableSqlBuilder<TEntity> : ReplaceableSqlBuilder, IReplaceable<TEntity>
    {
        private readonly List<TableFieldInfoForSqlBuilder> _includeFieldsList = new();
        private object _parameter;

        private ReplaceableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {

        }

        /// <summary>
        /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IReplaceable<TEntity> Create(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            var sqlBuilder = new ReplaceableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
            if (autoIncludeFields)
            {
                sqlBuilder.IncludeFields(typeof(TEntity).GetAllFieldNames().ToArray());
            }
            return sqlBuilder;
        }

        #region [Field]
        public virtual IReplaceable<TEntity> IncludeFields(params string[] fields)
        {
            SqlBuilderUtil.IncludeFields(SqlAdapter, _includeFieldsList, fields);
            return this;
        }
        public virtual IReplaceable<TEntity> IgnoreFields(params string[] fields)
        {
            SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _includeFieldsList, fields);
            return this;
        }

        public virtual IReplaceable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IncludeFields(fields);
        }
        public virtual IReplaceable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IgnoreFields(fields);
        }
        #endregion

        public virtual IReplaceable<TEntity> SetParameter(object param)
        {
            _parameter = param;
            return this;
        }

        public virtual IReplaceableSql Build()
        {
            var fields = _includeFieldsList;
            if (!fields.Any())
                return default;

            var sb = new StringBuilder();
            var formatFields = fields.Select(fieldInfo => SqlAdapter.FormatFieldName(fieldInfo.FieldName));
            var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            switch (SqlAdapter.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    if (_parameter is IEnumerable<TEntity> entities && entities.Count() > 1)// BulkInsertOrUpdate
                    {
                        #region 解析批量新增的参数
                        var paramDic = new Dictionary<string, object>();
                        var index = 0;
                        var insertValueParams = new List<string>();
                        var formatParameterNames = new List<string>();
                        foreach (var entity in entities)
                        {
                            index++;
                            formatParameterNames.Clear();
                            foreach (var field in fields)
                            {
                                var fieldInfo = tableFieldInfos.Find(c => c.FieldName == field.FieldName);
                                if (fieldInfo == null)
                                {
                                    throw new InvalidOperationException($"Table [{field.TableName}] field [{field.FieldName}] not found in [{typeof(TEntity).FullName}].");
                                }
                                var parameterName = ConditionBuilder.UniqueParameter($"{fieldInfo.Property.Name}_{index}", paramDic);
                                formatParameterNames.Add(SqlAdapter.FormatInputParameter(parameterName));
                                paramDic.Add(parameterName, fieldInfo.Property.GetValue(entity, null));
                            }
                            insertValueParams.Add($"({string.Join(", ", formatParameterNames)})");
                        }

                        var bulkInsertValuesString = string.Join(",", insertValueParams);
                        SetParameter(paramDic);
                        #endregion

                        sb.Append(string.Format(SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), bulkInsertValuesString));
                    }
                    else
                    {
                        var formatParameters = fields.Select(fieldInfo =>
                        {
                            var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);
                            var parameterName = findFieldInfo?.Property.Name ?? fieldInfo.FieldName;
                            return SqlAdapter.FormatInputParameter(parameterName);
                        });
                        sb.Append(string.Format(SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), $"({string.Join(", ", formatParameters)})"));
                    }
                    break;
                default:
                    throw new NotSupportedException($"[{SqlAdapter.DbType}]The database does not support the 'REPLACE INTO' SQL syntax.");
            }

            var replaceableSql = new DefaultReplaceableSql
            {
                Sql = sb.ToString(),
                Parameter = _parameter
            };
            return replaceableSql;
        }
    }

    public interface IReplaceable
    {
        ISqlAdapter SqlAdapter { get; }

        /// <summary>
        /// 创建新增或更新数据的SQL：<see cref="ReplaceableSqlBuilder.SqlTemplate"/>
        /// </summary>
        /// <returns></returns>
        IReplaceableSql Build();
    }

    public interface IReplaceable<TEntity> : IReplaceable
    {
        #region [Field]
        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IReplaceable<TEntity> IncludeFields(params string[] fields);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IReplaceable<TEntity> IgnoreFields(params string[] fields);

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        IReplaceable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IReplaceable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
        #endregion

        /// <summary>
        /// 设置SQL入参
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        IReplaceable<TEntity> SetParameter(object param);
    }
}
