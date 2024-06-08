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
    public static string GetFieldName(this MemberInfo memberInfo)
    {
        if (memberInfo.GetCustomAttributes<NotMappedAttribute>(false).Any())
        {
            throw new InvalidOperationException($"The member [{memberInfo.DeclaringType?.Name}.{memberInfo.Name}] is not a database table field.");
        }

        var fieldName = memberInfo.GetCustomAttributes<ColumnAttribute>(false).FirstOrDefault()?.Name;
        return !string.IsNullOrWhiteSpace(fieldName) ? fieldName : memberInfo.Name.ToNamingConvention(DbContextConfiguration.Options.DefaultNamingConvention);
    }

    public static string GetSequenceName(this MemberInfo memberInfo)
    {
        return memberInfo.GetCustomAttributes<SequenceAttribute>(false).FirstOrDefault()?.Name;
    }
}