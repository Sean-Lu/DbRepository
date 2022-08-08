using System;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public class Table<TEntity> where TEntity : class
    {
        public static string Field(Expression<Func<TEntity, object>> fieldExpression)
        {
            return fieldExpression.GetMemberNames()?.FirstOrDefault();
        }

        public static string[] Fields(Expression<Func<TEntity, object>> fieldExpression)
        {
            return fieldExpression.GetMemberNames()?.ToArray();
        }

        /// <summary>
        /// 获取主表表名
        /// </summary>
        /// <returns></returns>
        public static string TableName()
        {
            return typeof(TEntity).GetMainTableName();
        }
    }
}
