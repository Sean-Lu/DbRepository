using System;
using System.Data.Common;
using System.Linq;

namespace Sean.Core.DbRepository.Extensions
{
    public static class DatabaseTypeExtensions
    {
        public static DbProviderFactory GetDbProviderFactory(this DatabaseType dbType)
        {
            return DbProviderFactoryManager.GetDbProviderFactory(dbType);
        }

        #region DbProviderMap
        public static DbProviderMap GetDbProviderMap(this DatabaseType type)
        {
            DbProviderFactoryManager.DbProviderMapDic.TryGetValue(type, out var providerMap);
            return providerMap;
        }

        public static void SetDbProviderMap(this DatabaseType type, DbProviderMap providerMap)
        {
            if (providerMap != null)
            {
                if (!string.IsNullOrWhiteSpace(providerMap.ProviderInvariantName))
                {
                    if (DbProviderFactoryManager.DbProviderMapDic.Any(c => c.Key != type && c.Value != null && c.Value.ProviderInvariantName == providerMap.ProviderInvariantName))
                    {
                        throw new Exception($"{nameof(DbProviderMap)}.{nameof(DbProviderMap.ProviderInvariantName)} 的值不能重复：{providerMap.ProviderInvariantName}");
                    }

#if NETSTANDARD2_1 || NET5_0
                    if (!string.IsNullOrWhiteSpace(providerMap.FactoryTypeAssemblyQualifiedName))
                    {
                        DbProviderFactoryManager.RegisterFactory(providerMap.ProviderInvariantName, providerMap.FactoryTypeAssemblyQualifiedName);
                    }
#endif
                }
            }
            DbProviderFactoryManager.DbProviderMapDic.AddOrUpdate(type, providerMap, (_, _) => providerMap);
        }
        #endregion

        /// <summary>
        /// Mark as SQL input parameter.
        /// </summary>
        /// <param name="databaseType"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static string MarkAsSqlInputParameter(this DatabaseType databaseType, string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(parameter));

            if (parameter.StartsWith("["))
            {
                parameter = parameter.Trim('[');
            }
            else if (parameter.StartsWith("`"))
            {
                parameter = parameter.Trim('`');
            }

            switch (databaseType)
            {
                case DatabaseType.Oracle:
                    return $":{parameter}";
                default:
                    return $"@{parameter}";
            }
        }

        /// <summary>
        /// Mark as table or field name to avoid conflict with keyword.
        /// </summary>
        /// <param name="databaseType"></param>
        /// <param name="tableOrFieldName"></param>
        /// <returns></returns>
        public static string MarkAsTableOrFieldName(this DatabaseType databaseType, string tableOrFieldName)
        {
            if (string.IsNullOrWhiteSpace(tableOrFieldName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableOrFieldName));

            if (tableOrFieldName.StartsWith("[")
                || tableOrFieldName.StartsWith("`")
                || tableOrFieldName.StartsWith("\"")
                || tableOrFieldName.Contains(".")// example: SELECT a.FieldName FROM TableName a
                || tableOrFieldName.Contains(" ")// example: SELECT FieldName AS Alias FROM TableName
                || tableOrFieldName.Contains("(")// example: SELECT COUNT(FieldName) FROM TableName
                )
            {
                return tableOrFieldName;
            }

            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    return $"[{tableOrFieldName}]";
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    return $"`{tableOrFieldName}`";
                case DatabaseType.PostgreSql:
                    return $"\"{tableOrFieldName}\"";
                default:
                    return tableOrFieldName;
            }
        }

        /// <summary>
        /// SQL语句：表是否存在
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        public static string GetSqlForIsTableExists(this DatabaseType dbType, string dbName, string tableName)
        {
            switch (dbType)
            {
                case DatabaseType.MySql:
                    return $"SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = '{dbName}' AND table_name = '{tableName}';";
                case DatabaseType.SqlServer:
                    return $"SELECT COUNT(1) FROM sys.tables WHERE type = 'u' AND name='{tableName}';";
                case DatabaseType.Oracle:
                    return $"SELECT COUNT(1) FROM user_tables WHERE table_name='{tableName}';";
                case DatabaseType.SQLite:
                    return $"SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name='{tableName}';";
                case DatabaseType.Access:
                    return $"SELECT COUNT(1) FROM MsysObjects WHERE type = 1 AND name = '{tableName}';";
                case DatabaseType.PostgreSql:
                    return $"SELECT COUNT(1) FROM pg_class WHERE relname = '{tableName}';";
                default:
                    throw new NotSupportedException($"[{nameof(GetSqlForIsTableExists)}]-[{dbType}]");
            }
        }

        /// <summary>
        /// SQL语句：表字段是否存在
        /// </summary>
        /// <param name="dbType">数据库类型</param>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns></returns>
        public static string GetSqlForIsFieldExists(this DatabaseType dbType, string dbName, string tableName, string fieldName)
        {
            switch (dbType)
            {
                case DatabaseType.MySql:
                    return $"SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = '{dbName}' AND table_name = '{tableName}' AND column_name = '{fieldName}';";
                case DatabaseType.SqlServer:
                    return $"SELECT COUNT(1) FROM sys.columns WHERE object_id = object_id('{tableName}') AND name='{fieldName}';";
                case DatabaseType.Oracle:
                    return $"SELECT COUNT(1) FROM user_tab_columns WHERE table_name='{tableName}' AND column_name='{fieldName}';";
                case DatabaseType.SQLite:
                    return $"PRAGMA table_info('{tableName}');";
                case DatabaseType.PostgreSql:
                    return $"SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = '{dbName}' AND table_name = '{tableName}' AND column_name = '{fieldName}'";
                default:
                    throw new NotSupportedException($"[{nameof(GetSqlForIsFieldExists)}]-[{dbType}]");
            }
        }
    }
}
