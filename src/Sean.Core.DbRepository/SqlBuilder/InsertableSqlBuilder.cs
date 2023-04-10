using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository;

public abstract class InsertableSqlBuilder : BaseSqlBuilder
{
    public const string SqlTemplate = "INSERT INTO {0}({1}) VALUES{2}";
    public const string SqlIndentedTemplate = @"INSERT INTO {0}({1}) 
VALUES{2}";

    protected InsertableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
    {
    }
}

public class InsertableSqlBuilder<TEntity> : InsertableSqlBuilder, IInsertable<TEntity>
{
    private readonly List<TableFieldInfoForSqlBuilder> _includeFieldsList = new();
    private bool _returnLastInsertId;
    private object _parameter;

    private InsertableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
    {

    }

    /// <summary>
    /// Create an instance of <see cref="IInsertable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
    /// <param name="tableName">The table name.</param>
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
        SqlBuilderUtil.IncludeFields(SqlAdapter, _includeFieldsList, fields);
        return this;
    }
    public virtual IInsertable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _includeFieldsList, fields);
        return this;
    }
    public virtual IInsertable<TEntity> IdentityFields(params string[] fields)
    {
        SqlBuilderUtil.IdentityFields(SqlAdapter, _includeFieldsList, fields);
        return this;
    }

    public virtual IInsertable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IncludeFields(fields);
    }
    public virtual IInsertable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IgnoreFields(fields);
    }
    public virtual IInsertable<TEntity> IdentityFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IdentityFields(fields);
    }
    #endregion

    public virtual IInsertable<TEntity> ReturnAutoIncrementId(bool returnAutoIncrementId = true)
    {
        _returnLastInsertId = returnAutoIncrementId;
        return this;
    }

    public virtual IInsertable<TEntity> SetParameter(object param)
    {
        _parameter = param;
        return this;
    }

    public virtual ISqlCommand Build()
    {
        CheckIncludeIdentityFields();

        var fields = _includeFieldsList.Where(c => !c.Identity).ToList();
        if (!fields.Any())
            return default;

        var sb = new StringBuilder();
        var formatFields = fields.Select(fieldInfo => SqlAdapter.FormatFieldName(fieldInfo.FieldName));
        var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
        if (_parameter is IEnumerable<TEntity> entities)// BulkInsert
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
                    var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == field.FieldName);
                    if (findFieldInfo == null)
                    {
                        throw new InvalidOperationException($"Table [{field.TableName}] field [{field.FieldName}] not found in [{typeof(TEntity).FullName}].");
                    }

                    if (!BaseSqlBuilder.SqlParameterized)
                    {
                        var property = findFieldInfo.Property;
                        if (property != null)
                        {
                            var value = property.GetValue(entity);
                            var convertResult = SqlBuilderUtil.ConvertToSqlString(value, property.PropertyType, out var convertable);
                            if (convertable)
                            {
                                formatParameterNames.Add(convertResult);
                                continue;
                            }
                        }
                    }

                    var parameterName = ConditionBuilder.UniqueParameter($"{findFieldInfo.Property.Name}_{index}", paramDic);
                    formatParameterNames.Add(SqlAdapter.FormatInputParameter(parameterName));
                    paramDic.Add(parameterName, findFieldInfo.Property.GetValue(entity, null));
                }
                insertValueParams.Add($"({string.Join(", ", formatParameterNames)})");
            }

            var bulkInsertValuesString = string.Join($", {(BaseSqlBuilder.SqlIndented ? Environment.NewLine : string.Empty)}", insertValueParams);
            SetParameter(paramDic);
            #endregion

            sb.Append(string.Format(BaseSqlBuilder.SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), bulkInsertValuesString));
        }
        else
        {
            var formatParameters = fields.Select(fieldInfo =>
            {
                var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);

                if (!BaseSqlBuilder.SqlParameterized)
                {
                    var property = findFieldInfo?.Property;
                    if (property != null)
                    {
                        var value = property.GetValue(_parameter);
                        var convertResult = SqlBuilderUtil.ConvertToSqlString(value, property.PropertyType, out var convertable);
                        if (convertable)
                        {
                            return convertResult;
                        }
                    }
                }

                var parameterName = findFieldInfo?.Property.Name ?? fieldInfo.FieldName;
                return SqlAdapter.FormatInputParameter(parameterName);
            });
            sb.Append(string.Format(BaseSqlBuilder.SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), $"({string.Join(", ", formatParameters)})"));
        }

        if (_returnLastInsertId)
        {
            switch (SqlAdapter.DbType)
            {
                case DatabaseType.Oracle:
                    var sequenceName = typeof(TEntity).GetEntityInfo()?.SequenceName;
                    sb.Append($";{string.Format(SqlAdapter.DbType.GetSqlForGetLastIdentityId(), sequenceName)}");
                    break;
                default:
                    sb.Append($";{SqlAdapter.DbType.GetSqlForGetLastIdentityId()}");
                    break;
            }
        }

        var sql = new DefaultSqlCommand
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }

    private void CheckIncludeIdentityFields()
    {
        if (_parameter != null && _includeFieldsList.Any(c => c.Identity))
        {
            var identityFieldInfos = _includeFieldsList.Where(c => c.Identity);
            var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            if (_parameter is IEnumerable<TEntity> entities)
            {
                foreach (var identityFieldInfo in identityFieldInfos)
                {
                    var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == identityFieldInfo.FieldName);
                    var property = findFieldInfo?.Property;
                    if (property != null && entities.Any())
                    {
                        var value = property.GetValue(entities.FirstOrDefault());
                        var defaultValue = property.PropertyType.GetDefaultValue();
                        if (!Equals(value, defaultValue))
                        {
                            identityFieldInfo.Identity = false;
                        }
                    }
                }
            }
            else
            {
                foreach (var identityFieldInfo in identityFieldInfos)
                {
                    var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == identityFieldInfo.FieldName);
                    var property = findFieldInfo?.Property;
                    if (property != null)
                    {
                        var value = property.GetValue(_parameter);
                        var defaultValue = property.PropertyType.GetDefaultValue();
                        if (!Equals(value, defaultValue))
                        {
                            identityFieldInfo.Identity = false;
                        }
                    }
                }
            }
        }
    }
}

public interface IInsertable
{
    ISqlAdapter SqlAdapter { get; }

    /// <summary>
    /// 创建新增数据的SQL：<see cref="InsertableSqlBuilder.SqlTemplate"/>
    /// </summary>
    /// <returns></returns>
    ISqlCommand Build();
}

public interface IInsertable<TEntity> : IInsertable
{
    #region [Field]
    /// <summary>
    /// 包含字段
    /// </summary>
    /// <param name="fields">字段名称</param>
    /// <returns></returns>
    IInsertable<TEntity> IncludeFields(params string[] fields);
    /// <summary>
    /// 忽略字段
    /// </summary>
    /// <param name="fields">字段名称</param>
    /// <returns></returns>
    IInsertable<TEntity> IgnoreFields(params string[] fields);
    /// <summary>
    /// 自增字段
    /// </summary>
    /// <param name="fields">字段名称</param>
    /// <returns></returns>
    IInsertable<TEntity> IdentityFields(params string[] fields);

    /// <summary>
    /// 包含字段
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IInsertable<TEntity> IncludeFields(Expression<Func<TEntity, object>> fieldExpression);
    /// <summary>
    /// 忽略字段
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IInsertable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression);
    /// <summary>
    /// 自增字段
    /// </summary>
    /// <param name="fieldExpression"></param>
    /// <returns></returns>
    IInsertable<TEntity> IdentityFields(Expression<Func<TEntity, object>> fieldExpression);
    #endregion

    /// <summary>
    /// 返回自增id
    /// </summary>
    /// <param name="returnAutoIncrementId"></param>
    /// <returns></returns>
    IInsertable<TEntity> ReturnAutoIncrementId(bool returnAutoIncrementId = true);

    /// <summary>
    /// 设置SQL入参
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    IInsertable<TEntity> SetParameter(object param);
}