using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class ReplaceableSqlBuilder<TEntity> : BaseSqlBuilder<TEntity, IReplaceable<TEntity>>, IReplaceable<TEntity>
{
    private const string SqlTemplate = "REPLACE INTO {0}({1}) VALUES{2}";
    private const string SqlIndentedTemplate = @"REPLACE INTO {0}({1}) 
VALUES{2}";

    private readonly List<TableFieldInfoForSqlBuilder> _tableFieldList = new();
    private object _parameter;
    private IReadOnlyList<TEntity> _bulkEntities;

    private ReplaceableSqlBuilder(DatabaseType dbType) : base(dbType)
    {

    }

    /// <summary>
    /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
    /// </summary>
    /// <returns></returns>
    public static IReplaceable<TEntity> Create()
    {
        return new ReplaceableSqlBuilder<TEntity>(DatabaseType.Unknown);
    }
    /// <summary>
    /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <returns></returns>
    public static IReplaceable<TEntity> Create(DatabaseType dbType)
    {
        return new ReplaceableSqlBuilder<TEntity>(dbType);
    }

    #region [Field]
    public virtual IReplaceable<TEntity> InsertFields(params string[] fields)
    {
        SqlBuilderUtil.IncludeFields(TableName, _tableFieldList, fields);
        return this;
    }
    public virtual IReplaceable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(TableName, _tableFieldList, fields);
        return this;
    }

    public virtual IReplaceable<TEntity> InsertFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null)
        {
            return this;
        }
        var fields = fieldExpression.GetFieldNames().ToArray();
        return InsertFields(fields);
    }
    public virtual IReplaceable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null)
        {
            return this;
        }
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IgnoreFields(fields);
    }
    #endregion

    public virtual IReplaceable<TEntity> SetParameter(object param)
    {
        _parameter = param;
        _bulkEntities = null;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        if (!_tableFieldList.Any())
        {
            SqlBuilderUtil.IncludeFields<TEntity>(_tableFieldList);
        }

        var fields = _tableFieldList;
        var sb = new StringBuilder();
        var formatFields = fields.Select(fieldInfo => SqlAdapter.FormatFieldName(fieldInfo.FieldName)).ToList();
        var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
        var fieldMappings = fields.Select(field => new
        {
            Field = field,
            EntityField = tableFieldInfos.Find(c => c.FieldName == field.FieldName)
        }).ToList();
        object commandParameter = _parameter;
        switch (SqlAdapter.DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.OceanBase:
            case DatabaseType.SQLite:
                var bulkEntities = GetBulkEntities();
                if (bulkEntities?.Count == 0)
                {
                    return default;
                }
                if (bulkEntities != null)// BulkInsertOrUpdate
                {
                    #region 解析批量新增的参数
                    var paramDic = new Dictionary<string, object>();
                    var index = 0;
                    var insertValueParams = new List<string>();
                    var formatParameterNames = new List<string>();
                    foreach (var entity in bulkEntities)
                    {
                        index++;
                        formatParameterNames.Clear();
                        foreach (var fieldMapping in fieldMappings)
                        {
                            var findFieldInfo = fieldMapping.EntityField;
                            if (findFieldInfo == null)
                            {
                                throw new InvalidOperationException($"Table [{fieldMapping.Field.TableName}] field [{fieldMapping.Field.FieldName}] not found in [{typeof(TEntity).FullName}].");
                            }

                            if (!SqlParameterized)
                            {
                                var property = findFieldInfo.Property;
                                if (property != null)
                                {
                                    var value = property.GetValue(entity);
                                    var convertResult = SqlBuilderUtil.ConvertToSqlString(SqlAdapter.DbType, value, out var convertible);
                                    if (convertible)
                                    {
                                        formatParameterNames.Add(convertResult);
                                        continue;
                                    }
                                }
                            }

                            var parameterName = ConditionBuilder.UniqueParameter($"{findFieldInfo.Property.Name}_{index}", paramDic);
                            formatParameterNames.Add(SqlAdapter.FormatSqlParameter(parameterName));
                            paramDic.Add(parameterName, findFieldInfo.Property.GetValue(entity, null));
                        }
                        insertValueParams.Add($"({string.Join(", ", formatParameterNames)})");
                    }

                    var bulkInsertValuesString = string.Join($", {(SqlIndented ? Environment.NewLine : string.Empty)}", insertValueParams);
                    commandParameter = paramDic;
                    #endregion

                    sb.Append(string.Format(SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), bulkInsertValuesString));
                }
                else
                {
                    var formatParameters = fieldMappings.Select(fieldMapping =>
                    {
                        var findFieldInfo = fieldMapping.EntityField;

                        if (!SqlParameterized)
                        {
                            var property = findFieldInfo?.Property;
                            if (property != null)
                            {
                                var value = property.GetValue(_parameter);
                                var convertResult = SqlBuilderUtil.ConvertToSqlString(SqlAdapter.DbType, value, out var convertible);
                                if (convertible)
                                {
                                    return convertResult;
                                }
                            }
                        }

                        var parameterName = findFieldInfo?.Property?.Name ?? fieldMapping.Field.FieldName;
                        return SqlAdapter.FormatSqlParameter(parameterName);
                    }).ToList();
                    sb.Append(string.Format(SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), $"({string.Join(", ", formatParameters)})"));
                }
                break;
            case DatabaseType.Oracle:
            case DatabaseType.QuestDB:
            case DatabaseType.DuckDB:
            case DatabaseType.Dameng:
            case DatabaseType.Xugu:
            default:
                throw new NotSupportedException($"[{SqlAdapter.DbType}]The database does not support the 'REPLACE INTO' SQL syntax.");
        }

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString(),
            Parameter = commandParameter
        };
        return sql;
    }

    private IReadOnlyList<TEntity> GetBulkEntities()
    {
        if (!(_parameter is IEnumerable<TEntity> entities))
        {
            return null;
        }

        // 保留原始参数，参数字典只属于本次命令；缓存实体快照可保证一次性枚举器和重复 Build 均稳定。
        if (_bulkEntities != null)
        {
            return _bulkEntities;
        }

        _bulkEntities = entities.ToList();
        return _bulkEntities;
    }
}
