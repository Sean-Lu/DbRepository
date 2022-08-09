using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Sean.Core.DbRepository.Extensions
{
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// 获取数据库表字段名称
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public static string GetFieldName(this MemberInfo memberInfo)
        {
            var fieldAttribute = memberInfo.GetCustomAttributesExt<ColumnAttribute>(false)?.FirstOrDefault();
            if (fieldAttribute != null)
            {
                return fieldAttribute.Name;
            }

            return memberInfo.Name;
        }

        public static IEnumerable<T> GetCustomAttributesExt<T>(this MemberInfo memberInfo, bool inherit) where T : Attribute
        {
#if NET40
            return memberInfo.GetCustomAttributes(typeof(T), inherit).Select(c => c as T);
#else
            return memberInfo.GetCustomAttributes<T>(inherit);
#endif
        }
    }
}
