using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Sean.Core.DbRepository.Extensions;

/// <summary>
/// 实体属性与查询结果列的内部映射规则。
/// </summary>
internal static class EntityPropertyMapping
{
    public static PropertyInfo[] Create(Type entityType, IReadOnlyList<string> columnNames)
    {
        // 查询只填充当前实体，不能通过同名结果列改写整个类型的静态状态。
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0)
            .ToList();
        var mappings = new PropertyInfo[columnNames.Count];

        for (var index = 0; index < columnNames.Count; index++)
        {
            var columnName = columnNames[index];

            // 显式数据库字段名优先，避免同名属性抢占带 Column 特性的目标属性。
            mappings[index] = properties.FirstOrDefault(property =>
            {
                var column = property.GetCustomAttributes(typeof(ColumnAttribute), true)
                    .OfType<ColumnAttribute>()
                    .FirstOrDefault();
                return !string.IsNullOrWhiteSpace(column?.Name)
                       && columnName.Equals(column.Name, StringComparison.OrdinalIgnoreCase);
            });

            // 保留原有按属性名映射的兼容行为；即使声明了 Column，也允许查询使用属性名别名。
            mappings[index] ??= properties.FirstOrDefault(property =>
                columnName.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
        }

        return mappings;
    }
}
