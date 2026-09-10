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

public class InsertableSqlBuilder<TEntity> : BaseSqlBuilder<TEntity, IInsertable<TEntity>>, IInsertable<TEntity>
{
    private const string SqlTemplate = "INSERT INTO {0}({1}) VALUES{2}";
    private const string SqlIndentedTemplate = @"INSERT INTO {0}({1}) 
VALUES{2}";

    private readonly List<TableFieldInfoForSqlBuilder> _tableFieldList = new();
    private bool _returnLastInsertId;
    private object _parameter;
    private IReadOnlyList<TEntity> _bulkEntities;
    private OutputParameterOptions _outputParameterOptions;

    private InsertableSqlBuilder(DatabaseType dbType) : base(dbType)
    {

    }

    /// <summary>
    /// Create an instance of <see cref="IInsertable{TEntity}"/>.
    /// </summary>
    /// <returns></returns>
    public static IInsertable<TEntity> Create()
    {
        return new InsertableSqlBuilder<TEntity>(DatabaseType.Unknown);
    }
    /// <summary>
    /// Create an instance of <see cref="IInsertable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <returns></returns>
    public static IInsertable<TEntity> Create(DatabaseType dbType)
    {
        return new InsertableSqlBuilder<TEntity>(dbType);
    }

    #region [Field]
    public virtual IInsertable<TEntity> InsertFields(params string[] fields)
    {
        SqlBuilderUtil.IncludeFields(TableName, _tableFieldList, fields);
        return this;
    }
    public virtual IInsertable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(TableName, _tableFieldList, fields);
        return this;
    }
    public virtual IInsertable<TEntity> IdentityFields(params string[] fields)
    {
        SqlBuilderUtil.IdentityFields(TableName, _tableFieldList, fields);
        return this;
    }

    public virtual IInsertable<TEntity> InsertFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null)
        {
            return this;
        }
        var fields = fieldExpression.GetFieldNames().ToArray();
        return InsertFields(fields);
    }
    public virtual IInsertable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null)
        {
            return this;
        }
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IgnoreFields(fields);
    }
    public virtual IInsertable<TEntity> IdentityFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null)
        {
            return this;
        }
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
        _bulkEntities = null;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        if (!_tableFieldList.Any())
        {
            SqlBuilderUtil.IncludeFields<TEntity>(_tableFieldList);
        }

        var bulkEntities = GetBulkEntities();
        if (bulkEntities?.Count == 0)
        {
            return default;
        }
        var fields = GetInsertFields(bulkEntities);
        if (!fields.Any())
            return default;

        var sb = new StringBuilder();
        var formatFields = fields.Select(fieldInfo => SqlAdapter.FormatFieldName(fieldInfo.FieldName)).ToList();
        var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
        var valuesBuilder = new WriteValuesBuilder<TEntity>(SqlAdapter, fields, SqlParameterized);
        object commandParameter = _parameter;
        var values = bulkEntities != null
            ? valuesBuilder.BuildBulk(bulkEntities, SqlIndented, out commandParameter)
            : valuesBuilder.BuildSingle(_parameter);
        sb.Append(string.Format(SqlIndented ? SqlIndentedTemplate : SqlTemplate,
            SqlAdapter.FormatTableName(), string.Join(", ", formatFields), values));

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
            Parameter = commandParameter,
            OutputParameterOptions = _outputParameterOptions
        };
        return sql;
    }

    private IReadOnlyList<TEntity> GetBulkEntities()
    {
        if (!(_parameter is IEnumerable<TEntity> entities))
        {
            return null;
        }

        // 一次性枚举器只能读取一次；缓存实体快照，确保身份字段检查、SQL 生成和重复 Build 使用同一批数据。
        if (_bulkEntities != null)
        {
            return _bulkEntities;
        }

        _bulkEntities = entities.ToList();
        return _bulkEntities;
    }

    private List<TableFieldInfoForSqlBuilder> GetInsertFields(IReadOnlyList<TEntity> bulkEntities)
    {
        if (_parameter == null || !_tableFieldList.Any(c => c.IsIdentityField))
        {
            return _tableFieldList.Where(c => !c.IsIdentityField).ToList();
        }

        var parameter = bulkEntities != null
            ? bulkEntities.FirstOrDefault()
            : _parameter;
        if (parameter == null)
        {
            return _tableFieldList.Where(c => !c.IsIdentityField).ToList();
        }

        var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
        return _tableFieldList.Where(fieldInfo =>
        {
            if (!fieldInfo.IsIdentityField)
            {
                return true;
            }

            var property = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName)?.Property;
            if (property == null)
            {
                return false;
            }

            var value = property.GetValue(parameter);
            return !Equals(value, property.PropertyType.GetDefaultValue());
        }).ToList();
    }
}
