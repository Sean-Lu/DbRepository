﻿using System;
using System.Data.Common;

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
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                if (!string.IsNullOrWhiteSpace(providerMap.ProviderInvariantName) && !string.IsNullOrWhiteSpace(providerMap.FactoryTypeAssemblyQualifiedName))
                {
                    DbProviderFactories.UnregisterFactory(providerMap.ProviderInvariantName);
                    DbProviderFactories.RegisterFactory(providerMap.ProviderInvariantName, providerMap.FactoryTypeAssemblyQualifiedName);
                }
#endif
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
        /// <param name="name"></param>
        /// <returns></returns>
        public static string MarkAsTableOrFieldName(this DatabaseType databaseType, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

            if (name.StartsWith("[")
                || name.StartsWith("`")
                || name.StartsWith("\"")
                || name.Contains(".")// example: SELECT a.FieldName FROM TableName a
                || name.Contains(" ")// example: SELECT FieldName AS Alias FROM TableName
                || name.Contains("(")// example: SELECT COUNT(FieldName) FROM TableName
                )
            {
                return name;
            }

            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    return $"[{name}]";
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                case DatabaseType.MsAccess:
                    return $"`{name}`";
                case DatabaseType.PostgreSql:
                case DatabaseType.Oracle:
                case DatabaseType.DB2:
                    return $"\"{name}\"";
                default:
                    return name;
            }
        }

        #region SQL
        public static string GetSqlForTableExists(this DatabaseType dbType, string schemaName, string tableName)
        {
            var sql = DbContextConfiguration.Options.GetSqlForTableExists?.Invoke(dbType, schemaName, tableName);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            switch (dbType)
            {
                case DatabaseType.MySql:
                    sql = $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_schema = '{schemaName}' AND table_name = '{tableName}'";
                    break;
                case DatabaseType.SqlServer:
                    sql = $"SELECT COUNT(*) AS TableCount FROM sys.tables WHERE type = 'u' AND name='{tableName}'";
                    break;
                case DatabaseType.Oracle:
                    //sql = $"SELECT COUNT(*) AS TableCount FROM all_tables WHERE owner = '{schemaName}' AND table_name = '{tableName}'";
                    sql = $"SELECT COUNT(*) AS TableCount FROM user_tables WHERE table_name='{tableName}'";
                    break;
                case DatabaseType.SQLite:
                    sql = $"SELECT COUNT(*) AS TableCount FROM sqlite_master WHERE type = 'table' AND name='{tableName}'";
                    break;
                case DatabaseType.MsAccess:
                    //sql = $"SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_SCHEMA='PUBLIC' AND TABLE_NAME='{tableName}'";
                    sql = $"SELECT COUNT(*) AS TableCount FROM MSysObjects WHERE Name='{tableName}' AND Type=1 AND Flags=0";
                    break;
                case DatabaseType.Firebird:
                    sql = $"SELECT COUNT(*) AS TableCount FROM RDB$RELATIONS WHERE RDB$RELATION_NAME = '{tableName}' AND RDB$OWNER_NAME = '{schemaName}'";
                    break;
                case DatabaseType.PostgreSql:
                    sql = $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_schema = '{schemaName}' AND table_name = '{tableName}'";
                    break;
                case DatabaseType.DB2:
                    sql = $"SELECT COUNT(*) AS TableCount FROM SYSIBM.SYSTABLES WHERE NAME = '{tableName}' AND CREATOR = '{schemaName}'";
                    break;
                case DatabaseType.Informix:
                    sql = $"SELECT COUNT(*) AS TableCount FROM systables WHERE tabname='{tableName}' AND creator='{schemaName}'";
                    break;
                case DatabaseType.ClickHouse:
                    sql = $"SELECT COUNT(*) AS TableCount FROM system.tables WHERE database = '{schemaName}' AND name = '{tableName}'";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
            return sql;
        }

        public static string GetSqlForTableFieldExists(this DatabaseType dbType, string schemaName, string tableName, string fieldName)
        {
            var sql = DbContextConfiguration.Options.GetSqlForTableFieldExists?.Invoke(dbType, schemaName, tableName, fieldName);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            switch (dbType)
            {
                case DatabaseType.MySql:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_schema = '{schemaName}' AND table_name = '{tableName}' AND column_name = '{fieldName}'";
                    break;
                case DatabaseType.SqlServer:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM sys.columns WHERE object_id = object_id('{tableName}') AND name='{fieldName}'";
                    break;
                case DatabaseType.Oracle:
                    //sql = $"SELECT COUNT(*) AS ColumnCount FROM all_tab_columns WHERE owner = '{schemaName}' AND table_name='{tableName}' AND column_name='{fieldName}'";
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM user_tab_columns WHERE table_name='{tableName}' AND column_name='{fieldName}'";
                    break;
                case DatabaseType.SQLite:
                    //sql = $"PRAGMA table_info('{tableName}')";
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM pragma_table_info('{tableName}') WHERE name='{fieldName}'";
                    break;
                case DatabaseType.MsAccess:
                    //sql = $"SELECT COUNT(*) AS ColumnCount FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='PUBLIC' AND TABLE_NAME='{tableName}' AND COLUMN_NAME='{fieldName}'";
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM MSysObjects INNER JOIN MSysColumns ON MSysObjects.Id = MSysColumns.Id WHERE MSysObjects.Name='{tableName}' AND MSysColumns.Name='{fieldName}' AND MSysObjects.Type=1 AND MSysObjects.Flags=0";
                    break;
                case DatabaseType.Firebird:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM RDB$RELATION_FIELDS WHERE RDB$RELATION_NAME = '{tableName}' AND RDB$FIELD_NAME = '{fieldName}'";
                    break;
                case DatabaseType.PostgreSql:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_schema = '{schemaName}' AND table_name = '{tableName}' AND column_name = '{fieldName}'";
                    break;
                case DatabaseType.DB2:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM SYSIBM.SYSCOLUMNS WHERE TBNAME = '{tableName}' AND TBCREATOR = '{schemaName}' AND NAME = '{fieldName}'";
                    break;
                case DatabaseType.Informix:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM syscolumns WHERE tabid=(SELECT tabid FROM systables WHERE tabname='{tableName}' AND creator='{schemaName}') AND colname='{fieldName}'";
                    break;
                case DatabaseType.ClickHouse:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM system.columns WHERE database = '{schemaName}' AND table = '{tableName}' AND name = '{fieldName}'";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
            return sql;
        }

        public static string GetSqlForGetLastIdentityId(this DatabaseType dbType)
        {
            var sql = DbContextConfiguration.Options.GetSqlForGetLastIdentityId?.Invoke(dbType);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            switch (dbType)
            {
                case DatabaseType.MySql:
                    sql = "SELECT LAST_INSERT_ID() AS Id";
                    break;
                case DatabaseType.SqlServer:
                    //sql = "SELECT @@IDENTITY AS Id";// 返回为当前会话的所有作用域中的任何表最后生成的标识值
                    sql = "SELECT SCOPE_IDENTITY() AS Id";// 返回为当前会话和当前作用域中的任何表最后生成的标识值
                    break;
                case DatabaseType.Oracle:
                    sql = "SELECT \"{0}\".CURRVAL AS Id FROM dual";// {0} => sequence
                    break;
                case DatabaseType.SQLite:
                    sql = "SELECT LAST_INSERT_ROWID() AS Id";
                    break;
                case DatabaseType.MsAccess:
                    sql = "SELECT @@IDENTITY AS Id";
                    break;
                case DatabaseType.Firebird:
                    sql = "SELECT LAST_INSERT_ID() AS Id FROM RDB$DATABASE";
                    break;
                case DatabaseType.PostgreSql:
                    sql = "SELECT LASTVAL() AS Id";
                    break;
                case DatabaseType.DB2:
                    sql = "SELECT IDENTITY_VAL_LOCAL() AS Id FROM SYSIBM.SYSDUMMY1";
                    break;
                case DatabaseType.Informix:
                    sql = "SELECT dbinfo('sqlca.sqlerrd1') AS Id FROM systables WHERE tabid=1";
                    break;
                case DatabaseType.ClickHouse:
                    sql = "SELECT lastInsertId()";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
            return sql;
        }
        #endregion
    }
}
