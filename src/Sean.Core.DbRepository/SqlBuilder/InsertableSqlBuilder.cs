using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public abstract class InsertableSqlBuilder : BaseSqlBuilder
    {
        public const string SqlTemplate = "INSERT INTO {0}({1}) VALUES{2};";

        protected InsertableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {
        }
    }

    public class InsertableSqlBuilder<TEntity> : InsertableSqlBuilder, IInsertable<TEntity>
    {
        private readonly List<TableFieldInfoForSqlBuilder> _includeFieldsList = new();
        private bool _returnLastInsertId;
        private bool _bulkInsert;
        private object _parameter;

        private InsertableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
        {

        }

        /// <summary>
        /// Create an instance of <see cref="IInsertable{TEntity}"/>.
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="autoIncludeFields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IInsertable<TEntity> Create(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
        {
            var sqlBuilder = new InsertableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetMainTableName());
            if (autoIncludeFields)
            {
                sqlBuilder.IncludeFields(typeof(TEntity).GetAllFieldNames().ToArray());
                sqlBuilder.IdentityFields(typeof(TEntity).GetIdentityFieldNames().ToArray());
            }
            return sqlBuilder;
        }

        #region [Field]
        public virtual IInsertable<TEntity> IncludeFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (string.IsNullOrWhiteSpace(field)) continue;

                    if (!_includeFieldsList.Exists(c => c.TableName == SqlAdapter.TableName && c.FieldName == field))
                    {
                        _includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                        {
                            TableName = SqlAdapter.TableName,
                            FieldName = field
                        });
                    }
                }
            }
            return this;
        }
        public virtual IInsertable<TEntity> IgnoreFields(params string[] fields)
        {
            if (fields != null)
            {
                if (fields.Any() && !_includeFieldsList.Any())
                {
                    IncludeFields(typeof(TEntity).GetAllFieldNames().ToArray());
                }

                foreach (var field in fields)
                {
                    if (string.IsNullOrWhiteSpace(field)) continue;

                    _includeFieldsList.RemoveAll(c => c.TableName == SqlAdapter.TableName && c.FieldName == field);
                }
            }
            return this;
        }
        public virtual IInsertable<TEntity> IdentityFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (string.IsNullOrWhiteSpace(field)) continue;

                    var fieldInfo = _includeFieldsList.Find(c => c.TableName == SqlAdapter.TableName && c.FieldName == field);
                    if (fieldInfo != null)
                    {
                        fieldInfo.Identity = true;
                    }
                    else
                    {
                        _includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                        {
                            TableName = SqlAdapter.TableName,
                            FieldName = field,
                            Identity = true
                        });
                    }
                }
            }
            return this;
        }

        public virtual IInsertable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IncludeFields(fields);
        }
        public virtual IInsertable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IgnoreFields(fields);
        }
        public virtual IInsertable<TEntity> IdentityFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            if (fieldExpression == null) return this;
            var fields = fieldExpression.GetMemberNames().ToArray();
            return IdentityFields(fields);
        }
        #endregion

        public virtual IInsertable<TEntity> ReturnAutoIncrementId(bool returnAutoIncrementId = true)
        {
            _returnLastInsertId = returnAutoIncrementId;
            return this;
        }

        public virtual IInsertable<TEntity> BulkInsert(IEnumerable<TEntity> entities)
        {
            SetParameter(entities);
            _bulkInsert = true;
            return this;
        }

        public virtual IInsertable<TEntity> SetParameter(object param)
        {
            _parameter = param;
            return this;
        }

        public virtual IInsertableSql Build()
        {
            var fields = _includeFieldsList.Where(c => !c.Identity).ToList();
            if (!fields.Any())
                return default;

            var sb = new StringBuilder();
            var formatFields = fields.Select(fieldInfo => SqlAdapter.FormatFieldName(fieldInfo.FieldName));
            var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            if (_bulkInsert && _parameter is IEnumerable<TEntity> entities)
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

            if (_returnLastInsertId)
            {
                switch (SqlAdapter.DbType)
                {
                    case DatabaseType.Oracle:
                        var sequence = typeof(TEntity).GetEntityInfo()?.Sequence;
                        sb.Append(string.Format(SqlAdapter.GetSqlForSelectLastInsertId(), sequence));
                        break;
                    default:
                        sb.Append(SqlAdapter.GetSqlForSelectLastInsertId());
                        break;
                }
            }

            var insertableSql = new DefaultInsertableSql
            {
                InsertSql = sb.ToString(),
                Parameter = _parameter
            };
            return insertableSql;
        }
    }

    public interface IInsertable
    {
        ISqlAdapter SqlAdapter { get; }

        /// <summary>
        /// 创建SQL：新增数据
        /// </summary>
        /// <returns></returns>
        IInsertableSql Build();
    }

    public interface IInsertable<TEntity> : IInsertable
    {
        #region [Field]
        /// <summary>
        /// 包含字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IInsertable<TEntity> IncludeFields(params string[] fields);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IInsertable<TEntity> IgnoreFields(params string[] fields);
        /// <summary>
        /// 自增字段
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        IInsertable<TEntity> IdentityFields(params string[] fields);

        /// <summary>
        /// 包含字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        IInsertable<TEntity> IncludeFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
        /// <summary>
        /// 忽略字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IInsertable<TEntity> IgnoreFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
        /// <summary>
        /// 自增字段
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression"></param>
        /// <returns></returns>
        IInsertable<TEntity> IdentityFields<TProperty>(Expression<Func<TEntity, TProperty>> fieldExpression);
        #endregion

        /// <summary>
        /// 返回自增id
        /// </summary>
        /// <param name="returnAutoIncrementId"></param>
        /// <returns></returns>
        IInsertable<TEntity> ReturnAutoIncrementId(bool returnAutoIncrementId = true);

        /// <summary>
        /// 批量新增
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        IInsertable<TEntity> BulkInsert(IEnumerable<TEntity> entities);

        /// <summary>
        /// 设置SQL入参
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        IInsertable<TEntity> SetParameter(object param);
    }
}
