#if !NET40
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Attributes;

namespace Sean.Core.DbRepository.Extensions
{
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// 获取数据库表字段名称
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static string GetFieldName(this PropertyInfo propertyInfo)
        {
            var fieldAttribute = propertyInfo.GetCustomAttributes<FieldAttribute>(false).FirstOrDefault();
            if (fieldAttribute != null)
            {
                return fieldAttribute.Name;
            }

            return propertyInfo.Name;
        }
    }
}
#endif