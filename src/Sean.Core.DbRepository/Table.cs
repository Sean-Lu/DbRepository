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
            return fieldExpression.GetFieldNames()?.FirstOrDefault();
        }

        public static string[] Fields(Expression<Func<TEntity, object>> fieldExpression)
        {
            return fieldExpression.GetFieldNames()?.ToArray();
        }

        /// <summary>
        /// 获取主表表名
        /// </summary>
        /// <returns></returns>
        public static string TableName()
        {
            return typeof(TEntity).GetMainTableName();
        }

        public static string SqlWhereClause(DatabaseType databaseType, Expression<Func<TEntity, bool>> whereExpression)
        {
            var sqlAdapter = new DefaultSqlAdapter<TEntity>(databaseType);
            return SqlWhereClause(sqlAdapter, whereExpression);
        }
        public static string SqlWhereClause(ISqlAdapter sqlAdapter, Expression<Func<TEntity, bool>> whereExpression)
        {
            var sqlWhereClauseBuilder = SqlWhereClauseBuilder<TEntity>.Create(sqlAdapter)
                .Where(whereExpression);
            return sqlWhereClauseBuilder.GetParameterizedWhereClause();
        }
    }
}
