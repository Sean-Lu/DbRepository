using System;
using System.Reflection;

namespace Sean.Core.DbRepository.CodeFirst;

public interface ISqlGenerator
{
    string ConvertFieldType(PropertyInfo fieldPropertyInfo);
    string GetCreateTableSql<TEntity>(Func<string, string> tableNameFunc = null);
    string GetUpgradeSql<TEntity>(Func<string, string> tableNameFunc = null);
}