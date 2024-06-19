using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Sean.Core.DbRepository.Extensions;

public static class MemberInfoExtensions
{
    /// <summary>
    /// Gets the database table field name.
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    public static string GetFieldName(this MemberInfo memberInfo, NamingConvention namingConvention)
    {
        if (memberInfo.GetCustomAttributes<NotMappedAttribute>(true).Any())
        {
            throw new InvalidOperationException($"The member [{memberInfo.DeclaringType?.Name}.{memberInfo.Name}] is not a database table field.");
        }

        var fieldName = memberInfo.GetCustomAttributes<ColumnAttribute>(true).FirstOrDefault()?.Name;
        return !string.IsNullOrWhiteSpace(fieldName) ? fieldName : memberInfo.Name.ToNamingConvention(namingConvention);
    }

    public static string GetSequenceName(this MemberInfo memberInfo)
    {
        return memberInfo.GetCustomAttributes<SequenceAttribute>(true).FirstOrDefault()?.Name;
    }
}