using System;
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
                case DatabaseType.MsAccess:
                    return $"[{name}]";
                case DatabaseType.MySql:
                case DatabaseType.SQLite:
                    return $"`{name}`";
                case DatabaseType.PostgreSql:
                case DatabaseType.Oracle:
                case DatabaseType.DB2:
                case DatabaseType.Firebird:
                case DatabaseType.Informix:
                    return $"\"{name}\"";
                default:
                    return name;
            }
        }

        #region SQL
        public static string GetSqlForTableExists(this DatabaseType dbType, DbConnection connection, string tableName)
        {
            var sql = DbContextConfiguration.Options.GetSqlForTableExists?.Invoke(dbType, connection, tableName);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            switch (dbType)
            {
                case DatabaseType.MySql:
                    sql = $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'";
                    break;
                case DatabaseType.SqlServer:
                    sql = $"SELECT COUNT(*) AS TableCount FROM sys.tables WHERE type = 'u' AND name='{tableName}'";
                    break;
                case DatabaseType.Oracle:
                    //sql = $"SELECT COUNT(*) AS TableCount FROM all_tables WHERE owner = '{connection.Database}' AND table_name = '{tableName}'";
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
                    sql = $"SELECT COUNT(*) AS TableCount FROM RDB$RELATIONS WHERE RDB$RELATION_NAME = '{tableName}' AND RDB$VIEW_SOURCE IS NULL";
                    break;
                case DatabaseType.PostgreSql:
                    sql = $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'";
                    break;
                case DatabaseType.DB2:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserId");
                        if (property != null)
                        {
                            var schema = property.GetValue(connection, null) as string;
                            if (!string.IsNullOrEmpty(schema))
                            {
                                sql = $"SELECT COUNT(*) AS TableCount FROM SYSCAT.TABLES WHERE TABNAME='{tableName}' AND TABSCHEMA='{schema.ToUpper()}'";
                                break;
                            }
                        }

                        sql = $"SELECT COUNT(*) AS TableCount FROM SYSCAT.TABLES WHERE TABNAME='{tableName}'";
                        break;
                    }
                case DatabaseType.Informix:
                    sql = $"SELECT COUNT(*) AS TableCount FROM systables WHERE tabname='{tableName}' AND tabtype='T'";
                    break;
                case DatabaseType.ClickHouse:
                    sql = $"SELECT COUNT(*) AS TableCount FROM system.tables WHERE database = '{connection.Database}' AND name = '{tableName}'";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
            return sql;
        }

        public static string GetSqlForTableFieldExists(this DatabaseType dbType, DbConnection connection, string tableName, string fieldName)
        {
            var sql = DbContextConfiguration.Options.GetSqlForTableFieldExists?.Invoke(dbType, connection, tableName, fieldName);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            switch (dbType)
            {
                case DatabaseType.MySql:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}' AND column_name = '{fieldName}'";
                    break;
                case DatabaseType.SqlServer:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM sys.columns WHERE object_id = object_id('{tableName}') AND name='{fieldName}'";
                    break;
                case DatabaseType.Oracle:
                    //sql = $"SELECT COUNT(*) AS ColumnCount FROM all_tab_columns WHERE owner = '{connection.Database}' AND table_name='{tableName}' AND column_name='{fieldName}'";
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
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}' AND column_name = '{fieldName}'";
                    break;
                case DatabaseType.DB2:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserId");
                        if (property != null)
                        {
                            var schema = property.GetValue(connection, null) as string;
                            if (!string.IsNullOrEmpty(schema))
                            {
                                sql = $"SELECT COUNT(*) AS ColumnCount FROM SYSCAT.COLUMNS WHERE TABNAME='{tableName}' AND COLNAME='{fieldName}' AND TABSCHEMA='{schema.ToUpper()}'";
                                break;
                            }
                        }

                        sql = $"SELECT COUNT(*) AS ColumnCount FROM SYSCAT.COLUMNS WHERE TABNAME='{tableName}' AND COLNAME='{fieldName}'";
                        break;
                    }
                case DatabaseType.Informix:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM syscolumns WHERE tabid=(SELECT tabid FROM systables WHERE tabname='{tableName}' AND tabtype='T') AND colname='{fieldName}'";
                    break;
                case DatabaseType.ClickHouse:
                    sql = $"SELECT COUNT(*) AS ColumnCount FROM system.columns WHERE database = '{connection.Database}' AND table = '{tableName}' AND name = '{fieldName}'";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
            return sql;
        }
        #endregion
    }
}
