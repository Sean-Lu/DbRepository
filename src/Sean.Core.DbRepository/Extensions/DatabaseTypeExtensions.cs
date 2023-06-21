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
        public static string MarkAsSqlParameter(this DatabaseType databaseType, string parameter)
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
                case DatabaseType.DM:
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
                case DatabaseType.MariaDB:
                case DatabaseType.TiDB:
                case DatabaseType.OceanBase:
                case DatabaseType.SQLite:
                case DatabaseType.ClickHouse:
                    return $"`{name}`";
                case DatabaseType.PostgreSql:
                case DatabaseType.OpenGauss:
                case DatabaseType.HighgoDB:
                case DatabaseType.IvorySQL:
                case DatabaseType.Oracle:
                case DatabaseType.DB2:
                case DatabaseType.Firebird:
                case DatabaseType.Informix:
                case DatabaseType.DM:
                case DatabaseType.KingbaseES:
                case DatabaseType.ShenTong:
                case DatabaseType.Xugu:
                case DatabaseType.DuckDB:
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
                case DatabaseType.MariaDB:
                case DatabaseType.TiDB:
                case DatabaseType.OceanBase:
                case DatabaseType.PostgreSql:
                    return $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_schema='{connection.Database}' AND table_name='{tableName}'";
                case DatabaseType.OpenGauss:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserName");
                        var userName = property.GetValue(connection, null) as string;
                        return $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_catalog='{connection.Database}' AND table_schema='{userName}' AND table_name='{tableName}'";
                    }
                case DatabaseType.HighgoDB:
                case DatabaseType.IvorySQL:
                    return $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_type='BASE TABLE' AND  table_catalog='{connection.Database}' AND table_name='{tableName}'";
                case DatabaseType.ShenTong:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserID");
                        var userName = property.GetValue(connection, null) as string;
                        return $"SELECT COUNT(*) AS TableCount FROM INFO_SCHEM.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_CAT='{connection.Database}' AND TABLE_SCHEM='{userName.ToUpper()}' AND TABLE_NAME='{tableName}'";
                    }
                case DatabaseType.SqlServer:
                    return $"SELECT COUNT(*) AS TableCount FROM sys.tables WHERE type='u' AND name='{tableName}'";
                case DatabaseType.Oracle:
                    //return $"SELECT COUNT(*) AS TableCount FROM all_tables WHERE owner='{connection.Database}' AND table_name='{tableName}'";
                    return $"SELECT COUNT(*) AS TableCount FROM user_tables WHERE table_name='{tableName}'";
                case DatabaseType.SQLite:
                case DatabaseType.DuckDB:
                    return $"SELECT COUNT(*) AS TableCount FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                case DatabaseType.MsAccess:
                    //return $"SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_SCHEMA='PUBLIC' AND TABLE_NAME='{tableName}'";
                    return $"SELECT COUNT(*) AS TableCount FROM MSysObjects WHERE Name='{tableName}' AND Type=1 AND Flags=0";
                case DatabaseType.Firebird:
                    return $"SELECT COUNT(*) AS TableCount FROM RDB$RELATIONS WHERE RDB$RELATION_NAME='{tableName}' AND RDB$VIEW_SOURCE IS NULL";
                case DatabaseType.DB2:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserId");
                        if (property != null)
                        {
                            var schema = property.GetValue(connection, null) as string;
                            if (!string.IsNullOrEmpty(schema))
                            {
                                return $"SELECT COUNT(*) AS TableCount FROM SYSCAT.TABLES WHERE TABNAME='{tableName}' AND TABSCHEMA='{schema.ToUpper()}'";
                            }
                        }

                        return $"SELECT COUNT(*) AS TableCount FROM SYSCAT.TABLES WHERE TABNAME='{tableName}'";
                    }
                case DatabaseType.Informix:
                    return $"SELECT COUNT(*) AS TableCount FROM systables WHERE tabname='{tableName}' AND tabtype='T'";
                case DatabaseType.ClickHouse:
                    return $"SELECT COUNT(*) AS TableCount FROM system.tables WHERE database='{connection.Database}' AND name='{tableName}'";
                case DatabaseType.DM:
                    return $"SELECT COUNT(*) AS TableCount FROM user_tables WHERE table_name='{tableName}'";
                case DatabaseType.KingbaseES:
                    //return $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_schema='public' AND table_name='{tableName}'";
                    return $"SELECT COUNT(*) AS TableCount FROM information_schema.tables WHERE table_name='{tableName}'";
                case DatabaseType.Xugu:
                    return $"SELECT COUNT(*) AS TableCount FROM user_tables WHERE TABLE_NAME='{tableName}'";
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
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
                case DatabaseType.MariaDB:
                case DatabaseType.TiDB:
                case DatabaseType.OceanBase:
                case DatabaseType.PostgreSql:
                    return $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_schema='{connection.Database}' AND table_name='{tableName}' AND column_name='{fieldName}'";
                case DatabaseType.OpenGauss:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserName");
                        var userName = property.GetValue(connection, null) as string;
                        return $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_catalog='{connection.Database}' AND table_schema='{userName}' AND table_name='{tableName}' AND column_name='{fieldName}'";
                    }
                case DatabaseType.HighgoDB:
                case DatabaseType.IvorySQL:
                    return $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_catalog='{connection.Database}' AND table_name='{tableName}' AND column_name='{fieldName}'";
                case DatabaseType.ShenTong:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserID");
                        var userName = property.GetValue(connection, null) as string;
                        return $"SELECT COUNT(*) AS ColumnCount FROM INFO_SCHEM.\"COLUMNS\" WHERE TABLE_CAT='{connection.Database}' AND TABLE_SCHEM='{userName.ToUpper()}' AND TABLE_NAME='{tableName}' AND COLUMN_NAME='{fieldName}'";
                    }
                case DatabaseType.SqlServer:
                    return $"SELECT COUNT(*) AS ColumnCount FROM sys.columns WHERE object_id=object_id('{tableName}') AND name='{fieldName}'";
                case DatabaseType.Oracle:
                    //return $"SELECT COUNT(*) AS ColumnCount FROM all_tab_columns WHERE owner='{connection.Database}' AND table_name='{tableName}' AND column_name='{fieldName}'";
                    return $"SELECT COUNT(*) AS ColumnCount FROM user_tab_columns WHERE table_name='{tableName}' AND column_name='{fieldName}'";
                case DatabaseType.SQLite:
                case DatabaseType.DuckDB:
                    //return $"PRAGMA table_info('{tableName}')";
                    return $"SELECT COUNT(*) AS ColumnCount FROM pragma_table_info('{tableName}') WHERE name='{fieldName}'";
                case DatabaseType.MsAccess:
                    //return $"SELECT COUNT(*) AS ColumnCount FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='PUBLIC' AND TABLE_NAME='{tableName}' AND COLUMN_NAME='{fieldName}'";
                    return $"SELECT COUNT(*) AS ColumnCount FROM MSysObjects INNER JOIN MSysColumns ON MSysObjects.Id=MSysColumns.Id WHERE MSysObjects.Name='{tableName}' AND MSysColumns.Name='{fieldName}' AND MSysObjects.Type=1 AND MSysObjects.Flags=0";
                case DatabaseType.Firebird:
                    return $"SELECT COUNT(*) AS ColumnCount FROM RDB$RELATION_FIELDS WHERE RDB$RELATION_NAME='{tableName}' AND RDB$FIELD_NAME='{fieldName}'";
                case DatabaseType.DB2:
                    {
                        var connectionType = connection.GetType();
                        var property = connectionType.GetProperty("UserId");
                        if (property != null)
                        {
                            var schema = property.GetValue(connection, null) as string;
                            if (!string.IsNullOrEmpty(schema))
                            {
                                return $"SELECT COUNT(*) AS ColumnCount FROM SYSCAT.COLUMNS WHERE TABNAME='{tableName}' AND COLNAME='{fieldName}' AND TABSCHEMA='{schema.ToUpper()}'";
                            }
                        }

                        return $"SELECT COUNT(*) AS ColumnCount FROM SYSCAT.COLUMNS WHERE TABNAME='{tableName}' AND COLNAME='{fieldName}'";
                    }
                case DatabaseType.Informix:
                    return $"SELECT COUNT(*) AS ColumnCount FROM syscolumns WHERE tabid=(SELECT tabid FROM systables WHERE tabname='{tableName}' AND tabtype='T') AND colname='{fieldName}'";
                case DatabaseType.ClickHouse:
                    return $"SELECT COUNT(*) AS ColumnCount FROM system.columns WHERE database='{connection.Database}' AND table='{tableName}' AND name='{fieldName}'";
                case DatabaseType.DM:
                    return $"SELECT COUNT(*) AS ColumnCount FROM user_tab_columns WHERE table_name='{tableName}' AND column_name='{fieldName}'";
                case DatabaseType.KingbaseES:
                    //return $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_schema='public' AND table_name='{tableName}' AND column_name='{fieldName}'";
                    return $"SELECT COUNT(*) AS ColumnCount FROM information_schema.columns WHERE table_name='{tableName}' AND column_name='{fieldName}'";
                case DatabaseType.Xugu:
                    return $"SELECT COUNT(*) AS ColumnCount FROM user_columns WHERE TABLE_ID=(SELECT TABLE_ID FROM user_tables WHERE TABLE_NAME='{tableName}') and COL_NAME='{fieldName}'";
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
        }

        public static string GetSqlForSequenceExists(this DatabaseType dbType, DbConnection connection, string sequenceName)
        {
            //var sql = DbContextConfiguration.Options.GetSqlForSequenceExists?.Invoke(dbType, connection, sequenceName);
            //if (!string.IsNullOrWhiteSpace(sql))
            //{
            //    return sql;
            //}

            switch (dbType)
            {
                case DatabaseType.DuckDB:
                    return $"SELECT COUNT(*) AS SequenceCount FROM temp.pg_catalog.pg_sequences WHERE sequencename='{sequenceName}'";
                case DatabaseType.Xugu:
                    return $"SELECT COUNT(*) AS SequenceCount FROM user_sequences where seq_name='{sequenceName}'";
                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
        }
        #endregion
    }
}
