using System;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public static class Table<TEntity> where TEntity : class
    {
        public static string TableName(Func<string, string> tableNameFactory = null)
        {
            var tableName = typeof(TEntity).GetMainTableName();
            return tableNameFactory != null ? tableNameFactory(tableName) : tableName;
        }

        public static string Field(Expression<Func<TEntity, object>> fieldExpression, string alias = null)
        {
            return !string.IsNullOrWhiteSpace(alias) ? $"{fieldExpression.GetFieldNames()?.FirstOrDefault()} AS {alias}" : fieldExpression.GetFieldNames()?.FirstOrDefault();
        }

        public static string FieldWithTableName(Expression<Func<TEntity, object>> fieldExpression, string alias = null, Func<string, string> tableNameFactory = null)
        {
            return $"{TableName(tableNameFactory)}.{Field(fieldExpression, alias)}";
        }

        public static string[] Fields(Expression<Func<TEntity, object>> fieldExpression)
        {
            return fieldExpression.GetFieldNames()?.ToArray();
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
