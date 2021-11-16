using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sean.Core.DbRepository.Extensions
{
    public static class MemberInfoExtensions
    {
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
