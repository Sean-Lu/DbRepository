using System;
using System.Data.Common;
using System.Linq;
using Sean.Core.DbRepository.Config;

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
        /// Mark as input parameter.
        /// </summary>
        /// <param name="databaseType"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static string MarkAsInputParameter(this DatabaseType databaseType, string parameter)
        {
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
            if (!string.IsNullOrWhiteSpace(tableOrFieldName))
            {
                switch (databaseType)
                {
                    case DatabaseType.SqlServer:
                        if (!tableOrFieldName.StartsWith("[")) return $"[{tableOrFieldName}]";
                        break;
                    case DatabaseType.MySql:
                    case DatabaseType.SQLite:
                        if (!tableOrFieldName.StartsWith("`")) return $"`{tableOrFieldName}`";
                        break;
                }
            }
            return tableOrFieldName;
        }

        /// <summary>
        /// SQL语句：获取上一次插入id
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static string GetSqlForSelectLastInsertId(this DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.MySql:
                    return "SELECT LAST_INSERT_ID();";
                case DatabaseType.SqlServer:
                case DatabaseType.Access:
                    return "SELECT @@Identity;";
                case DatabaseType.SQLite:
                    return "SELECT last_insert_rowid();";
                default:
                    throw new NotSupportedException($"[{nameof(GetSqlForSelectLastInsertId)}]-[{dbType}]");
            }
        }

        /// <summary>
        /// SQL语句：表是否存在
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetSqlForIsTableExists(this DatabaseType dbType, string dbName, string tableName)
        {
            switch (dbType)
            {
                case DatabaseType.MySql:
                    return $"SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = '{dbName}' and table_name = '{tableName}';";
                case DatabaseType.SqlServer:
                    return $"SELECT COUNT(1) FROM sys.tables WHERE type = 'u' and name='{tableName}';";
                case DatabaseType.Oracle:
                    return $"SELECT COUNT(1) FROM user_tables WHERE table_name='{tableName}';";
                case DatabaseType.SQLite:
                    return $"SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' and name='{tableName}';";
                case DatabaseType.Access:
                    return $"SELECT COUNT(1) FROM MsysObjects WHERE type = 1 and name = '{tableName}';";
                default:
                    throw new NotSupportedException($"[{nameof(GetSqlForIsTableExists)}]-[{dbType}]");
            }
        }

        /// <summary>
        /// SQL语句：表字段是否存在
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static string GetSqlForIsFieldExists(this DatabaseType dbType, string dbName, string tableName, string fieldName)
        {
            switch (dbType)
            {
                case DatabaseType.MySql:
                    return $"SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = '{dbName}' and table_name = '{tableName}' and column_name = '{fieldName}';";
                case DatabaseType.SqlServer:
                    return $"SELECT COUNT(1) FROM sys.columns WHERE object_id = object_id('{tableName}') and name='{fieldName}';";
                case DatabaseType.Oracle:
                    return $"SELECT COUNT(1) FROM user_tab_columns WHERE table_name='{tableName}' and column_name='{fieldName}';";
                case DatabaseType.SQLite:
                    return $"PRAGMA table_info('{tableName}');";
                default:
                    throw new NotSupportedException($"[{nameof(GetSqlForIsFieldExists)}]-[{dbType}]");
            }
        }
    }
}
