using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

/// <summary>
/// 构建 INSERT、REPLACE 共用的 VALUES 部分；字段选择、实体快照及方言语法仍由调用方负责。
/// </summary>
internal sealed class WriteValuesBuilder<TEntity>
{
    private readonly ISqlAdapter _adapter;
    private readonly IReadOnlyList<TableFieldInfoForSqlBuilder> _fields;
    private readonly PropertyInfo[] _properties;
    private readonly bool _parameterized;

    internal WriteValuesBuilder(ISqlAdapter adapter, IReadOnlyList<TableFieldInfoForSqlBuilder> fields, bool parameterized)
    {
        _adapter = adapter;
        _fields = fields;
        _parameterized = parameterized;
        var entityFields = typeof(TEntity).GetEntityInfo().FieldInfos;
        // 按写入字段顺序解析一次，批量构建时不再逐行查找实体元数据。
        _properties = fields.Select(field => entityFields.Find(c => c.FieldName == field.FieldName)?.Property).ToArray();
    }

    internal string BuildSingle(object parameter)
    {
        var values = new List<string>();
        for (var i = 0; i < _fields.Count; i++)
        {
            var property = _properties[i];
            if (!_parameterized && property != null)
            {
                var literal = SqlBuilderUtil.ConvertToSqlString(_adapter.DbType, property.GetValue(parameter), out var convertible);
                if (convertible)
                {
                    values.Add(literal);
                    continue;
                }
            }

            // 单条命令允许匿名对象等外部参数，未映射字段使用字段名作为参数名。
            values.Add(_adapter.FormatSqlParameter(property?.Name ?? _fields[i].FieldName));
        }
        return $"({string.Join(", ", values)})";
    }

    internal string BuildBulk(IReadOnlyList<TEntity> entities, bool indented, out object commandParameter)
    {
        // 参数字典只属于本次命令，不能覆盖 Builder 保存的实体输入。
        var parameters = new Dictionary<string, object>();
        var rows = new List<string>();
        var values = new List<string>();
        var index = 0;
        foreach (var entity in entities)
        {
            index++;
            values.Clear();
            for (var i = 0; i < _fields.Count; i++)
            {
                var property = _properties[i];
                if (property == null)
                {
                    throw new InvalidOperationException($"Table [{_fields[i].TableName}] field [{_fields[i].FieldName}] not found in [{typeof(TEntity).FullName}].");
                }

                if (!_parameterized)
                {
                    var literal = SqlBuilderUtil.ConvertToSqlString(_adapter.DbType, property.GetValue(entity), out var convertible);
                    if (convertible)
                    {
                        values.Add(literal);
                        continue;
                    }
                }

                var name = ConditionBuilder.UniqueParameter($"{property.Name}_{index}", parameters);
                values.Add(_adapter.FormatSqlParameter(name));
                parameters.Add(name, property.GetValue(entity, null));
            }
            rows.Add($"({string.Join(", ", values)})");
        }

        commandParameter = parameters;
        return string.Join($", {(indented ? Environment.NewLine : string.Empty)}", rows);
    }
}
