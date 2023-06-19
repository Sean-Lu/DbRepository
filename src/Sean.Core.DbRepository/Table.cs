using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository;

public class Table<TEntity> where TEntity : class
{
    private readonly ISqlAdapter _sqlAdapter;

    public Table(DatabaseType databaseType)
    {
        _sqlAdapter = new DefaultSqlAdapter<TEntity>(databaseType);
    }
    public Table(ISqlAdapter sqlAdapter)
    {
        _sqlAdapter = sqlAdapter;
    }

    public string GetTableName(Func<string, string> tableNameFactory = null)
    {
        var tableName = _sqlAdapter.TableName ?? typeof(TEntity).GetMainTableName();
        return tableNameFactory != null ? tableNameFactory(tableName) : _sqlAdapter.FormatTableName(tableName);
    }

    public string GetField(Expression<Func<TEntity, object>> fieldExpression, string alias = null)
    {
        var fieldName = fieldExpression.GetFieldNames()?.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(alias) ? $"{_sqlAdapter.FormatFieldName(fieldName)} AS {alias}" : _sqlAdapter.FormatFieldName(fieldName);
    }

    public string GetFieldWithTableName(Expression<Func<TEntity, object>> fieldExpression, string alias = null, Func<string, string> tableNameFactory = null)
    {
        return $"{GetTableName(tableNameFactory)}.{GetField(fieldExpression, alias)}";
    }

    public string[] GetFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        return fieldExpression.GetFieldNames()?.Select(fieldName => _sqlAdapter.FormatFieldName(fieldName)).ToArray();
    }

    public string GetParameterizedWhereClause(Expression<Func<TEntity, bool>> whereExpression, IDictionary<string, object> parameters)
    {
        return whereExpression.GetParameterizedWhereClause(_sqlAdapter, parameters);
    }
    public string GetParameterizedWhereClause(Expression<Func<TEntity, bool>> whereExpression, out IDictionary<string, object> parameters)
    {
        return whereExpression.GetParameterizedWhereClause(_sqlAdapter, out parameters);

        //var sqlWhereClauseBuilder = SqlWhereClauseBuilder<TEntity>.Create(_sqlAdapter)
        //    .Where(whereExpression);
        //parameters = sqlWhereClauseBuilder.Parameter;
        //return sqlWhereClauseBuilder.GetParameterizedWhereClause();
    }

    public static Table<TEntity> Create(DatabaseType databaseType)
    {
        return new Table<TEntity>(databaseType);
    }
    public static Table<TEntity> Create(ISqlAdapter sqlAdapter)
    {
        return new Table<TEntity>(sqlAdapter);
    }

    public static string TableName(Func<string, string> tableNameFactory = null)
    {
        var tableName = typeof(TEntity).GetMainTableName();
        return tableNameFactory != null ? tableNameFactory(tableName) : tableName;
    }

    public static string Field(Expression<Func<TEntity, object>> fieldExpression, string alias = null)
    {
        var fieldName = fieldExpression.GetFieldNames()?.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(alias) ? $"{fieldName} AS {alias}" : fieldName;
    }

    public static string FieldWithTableName(Expression<Func<TEntity, object>> fieldExpression, string alias = null, Func<string, string> tableNameFactory = null)
    {
        return $"{TableName(tableNameFactory)}.{Field(fieldExpression, alias)}";
    }

    public static string[] Fields(Expression<Func<TEntity, object>> fieldExpression)
    {
        return fieldExpression.GetFieldNames()?.ToArray();
    }
}