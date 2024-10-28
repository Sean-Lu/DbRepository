using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;
using Sean.Utility.Extensions;

namespace Sean.Core.DbRepository;

public class InsertableSqlBuilder<TEntity> : BaseSqlBuilder<IInsertable<TEntity>>, IInsertable<TEntity>
{
    private const string SqlTemplate = "INSERT INTO {0}({1}) VALUES{2}";
    private const string SqlIndentedTemplate = @"INSERT INTO {0}({1}) 
VALUES{2}";

    private readonly List<TableFieldInfoForSqlBuilder> _tableFieldList = new();
    private bool _returnLastInsertId;
    private object _parameter;
    private OutputParameterOptions _outputParameterOptions;

    private InsertableSqlBuilder(DatabaseType dbType) : base(dbType, typeof(TEntity).GetEntityInfo().TableName)
    {

    }

    /// <summary>
    /// Create an instance of <see cref="IInsertable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
    /// <returns></returns>
    public static IInsertable<TEntity> Create(DatabaseType dbType, bool autoIncludeFields)
    {
        var sqlBuilder = new InsertableSqlBuilder<TEntity>(dbType);
        if (autoIncludeFields)
        {
            var entityInfo = typeof(TEntity).GetEntityInfo();
            sqlBuilder.InsertFields(entityInfo.FieldInfos.Select(c => c.FieldName).ToArray());
            sqlBuilder.IdentityFields(entityInfo.FieldInfos.Where(c => c.IsIdentityField).Select(c => c.FieldName).ToArray());
        }
        return sqlBuilder;
    }

    #region [Field]
    public virtual IInsertable<TEntity> InsertFields(params string[] fields)
    {
        SqlBuilderUtil.IncludeFields(SqlAdapter, _tableFieldList, fields);
        return this;
    }
    public virtual IInsertable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _tableFieldList, fields);
        return this;
    }
    public virtual IInsertable<TEntity> IdentityFields(params string[] fields)
    {
        SqlBuilderUtil.IdentityFields(SqlAdapter, _tableFieldList, fields);
        return this;
    }

    public virtual IInsertable<TEntity> InsertFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return InsertFields(fields);
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

    public virtual IInsertable<TEntity> OutputParameter(TEntity outputTarget, PropertyInfo outputPropertyInfo)
    {
        _outputParameterOptions = new OutputParameterOptions
        {
            OutputTarget = outputTarget,
            OutputPropertyInfo = outputPropertyInfo
        };
        return this;
    }

    public virtual IInsertable<TEntity> OutputParameterIF(bool condition, TEntity outputTarget, PropertyInfo outputPropertyInfo)
    {
        return condition ? OutputParameter(outputTarget, outputPropertyInfo) : this;
    }

    public virtual IInsertable<TEntity> SetParameter(object param)
    {
        _parameter = param;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        CheckIncludeIdentityFields();

        var fields = _tableFieldList.Where(c => !c.IsIdentityField).ToList();
        if (!fields.Any())
            return default;

        var sb = new StringBuilder();
        var formatFields = fields.Select(fieldInfo => SqlAdapter.FormatFieldName(fieldInfo.FieldName)).ToList();
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
            SetParameter(paramDic);
            #endregion

            sb.Append(string.Format(SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(TableName), string.Join(", ", formatFields), bulkInsertValuesString));
        }
        else
        {
            var formatParameters = fields.Select(fieldInfo =>
            {
                var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);

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

                var parameterName = findFieldInfo?.Property.Name ?? fieldInfo.FieldName;
                return SqlAdapter.FormatSqlParameter(parameterName);
            }).ToList();
            sb.Append(string.Format(SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(TableName), string.Join(", ", formatFields), $"({string.Join(", ", formatParameters)})"));
        }

        if (_returnLastInsertId)
        {
            switch (SqlAdapter.DbType)
            {
                case DatabaseType.MySql:
                case DatabaseType.MariaDB:
                case DatabaseType.TiDB:
                case DatabaseType.OceanBase:
                case DatabaseType.Dameng:
                case DatabaseType.ShenTong:
                    {
                        var returnIdSql = "SELECT LAST_INSERT_ID() AS Id";
                        sb.Append($";{returnIdSql}");
                        break;
                    }
                case DatabaseType.SQLite:
                    {
                        var returnIdSql = "SELECT LAST_INSERT_ROWID() AS Id";
                        sb.Append($";{returnIdSql}");
                        break;
                    }
                case DatabaseType.PostgreSql:
                case DatabaseType.OpenGauss:
                case DatabaseType.HighgoDB:
                case DatabaseType.IvorySQL:
                case DatabaseType.KingbaseES:
                    {
                        var returnIdSql = "SELECT LASTVAL() AS Id";
                        sb.Append($";{returnIdSql}");
                        break;
                    }
                case DatabaseType.SqlServer:
                    {
                        //var returnIdSql = "SELECT @@IDENTITY AS Id";// 返回为当前会话的所有作用域中的任何表最后生成的标识值
                        var returnIdSql = "SELECT SCOPE_IDENTITY() AS Id"; // 返回为当前会话和当前作用域中的任何表最后生成的标识值
                        sb.Append($";{returnIdSql}");
                        break;
                    }
                case DatabaseType.MsAccess:
                    {
                        var returnIdSql = "SELECT @@IDENTITY AS Id";
                        sb.Append($";{returnIdSql}");
                        break;
                    }
                //case DatabaseType.PostgreSql:// √
                //case DatabaseType.HighgoDB:// √
                //case DatabaseType.IvorySQL:// √
                case DatabaseType.DuckDB:
                case DatabaseType.Firebird:
                    {
                        var returnIdSql = $"RETURNING {SqlAdapter.FormatFieldName(_tableFieldList.FirstOrDefault(c => c.IsIdentityField).FieldName)}";
                        sb.Append($" {returnIdSql}");
                        break;
                    }
                case DatabaseType.Oracle:
                    {
                        var idPropInfo = _tableFieldList.FirstOrDefault(c => c.IsIdentityField);
                        var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == idPropInfo.FieldName);
                        var parameterName = findFieldInfo?.Property.Name ?? idPropInfo.FieldName;
                        var returnIdSql = $"RETURNING {SqlAdapter.FormatFieldName(idPropInfo.FieldName)} INTO {SqlAdapter.FormatSqlParameter(parameterName)}";
                        sb.Append($" {returnIdSql}");
                        break;
                    }
                case DatabaseType.DB2:
                    {
                        var returnIdSql = "SELECT IDENTITY_VAL_LOCAL() AS Id FROM SYSIBM.SYSDUMMY1";
                        sb.Append($";{returnIdSql}");
                        break;
                    }
                case DatabaseType.Informix:
                    {
                        var returnIdSql = $"SELECT dbinfo('sqlca.sqlerrd1') AS Id FROM systables WHERE tabname='{SqlAdapter.TableName}' AND tabtype='T'";
                        sb.Append($";{returnIdSql}");
                        break;
                    }
                case DatabaseType.Xugu:
                case DatabaseType.QuestDB:
                default:
                    throw new NotSupportedException($"[ReturnLastInsertId] Unsupported database type: {SqlAdapter.DbType}");
            }
        }

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString(),
            Parameter = _parameter,
            OutputParameterOptions = _outputParameterOptions
        };
        return sql;
    }

    private void CheckIncludeIdentityFields()
    {
        if (_parameter != null && _tableFieldList.Any(c => c.IsIdentityField))
        {
            var identityFieldInfos = _tableFieldList.Where(c => c.IsIdentityField).ToList();
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
                            identityFieldInfo.IsIdentityField = false;
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
                            identityFieldInfo.IsIdentityField = false;
                        }
                    }
                }
            }
        }
    }
}