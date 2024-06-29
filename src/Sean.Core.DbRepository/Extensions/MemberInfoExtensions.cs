using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

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
        if (memberInfo.IsNotMappedField())
        {
            throw new InvalidOperationException($"The member [{memberInfo.DeclaringType?.Name}.{memberInfo.Name}] is not a database table field.");
        }

        var fieldName = memberInfo.GetCustomAttribute<ColumnAttribute>(true)?.Name;
        return !string.IsNullOrWhiteSpace(fieldName) ? fieldName : memberInfo.Name.ToNamingConvention(namingConvention);
    }

    public static bool IsPrimaryKey(this MemberInfo memberInfo)
    {
        return memberInfo.GetCustomAttributes<KeyAttribute>(true).Any();
    }

    public static bool IsIdentityField(this MemberInfo memberInfo)
    {
        return memberInfo.GetCustomAttributes<DatabaseGeneratedAttribute>(true).Any(c => c.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity);
    }

    public static bool IsNotMappedField(this MemberInfo memberInfo)
    {
        return memberInfo.GetCustomAttributes<NotMappedAttribute>(true).Any();
    }
}