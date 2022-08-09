using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

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
            var fieldAttribute = propertyInfo.GetCustomAttributesExt<ColumnAttribute>(false)?.FirstOrDefault();
            if (fieldAttribute != null)
            {
                return fieldAttribute.Name;
            }

            return propertyInfo.Name;
        }

        /// <summary>
        /// 检查是否是静态成员
        /// </summary>
        /// <returns></returns>
        public static bool IsStaticProperty(this PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod != null)
            {
                return getMethod.IsStatic;
            }

            var setMethod = propertyInfo.GetSetMethod();
            return setMethod.IsStatic;
        }
    }
}
