using System.Data.Common;

namespace Sean.Core.DbRepository.Extensions
{
    public static class DbProviderFactoryExtensions
    {
        public static DatabaseType GetDatabaseType(this DbProviderFactory dbProviderFactory)
        {
            if (dbProviderFactory == null)
            {
                return DatabaseType.Unknown;
            }

            DatabaseType? databaseType = DbContextConfiguration.Options.MapToDatabaseType?.Invoke(dbProviderFactory);
            if (databaseType.HasValue && databaseType.Value != DatabaseType.Unknown)
            {
                return databaseType.Value;
            }

            var factoryName = dbProviderFactory.GetType().Name;
            switch (factoryName)
            {
                case "MySqlClientFactory":
                case "MySqlConnectorFactory":
                    databaseType = DatabaseType.MySql;
                    break;
                case "SqlClientFactory":
                    databaseType = DatabaseType.SqlServer;
                    break;
                case "OracleClientFactory":
                    databaseType = DatabaseType.Oracle;
                    break;
                case "SQLiteFactory":
                    databaseType = DatabaseType.SQLite;
                    break;
                case "DuckDBClientFactory":
                    databaseType = DatabaseType.DuckDB;
                    break;
                case "FirebirdClientFactory":
                    databaseType = DatabaseType.Firebird;
                    break;
                case "NpgsqlFactory":
                    databaseType = DatabaseType.PostgreSql;
                    break;
                case "OpenGaussFactory":
                    databaseType = DatabaseType.OpenGauss;
                    break;
                case "DB2Factory":
                    databaseType = DatabaseType.DB2;
                    break;
                case "IfxFactory":
                case "InformixClientFactory":
                    databaseType = DatabaseType.Informix;
                    break;
                case "ClickHouseConnectionFactory":
                    databaseType = DatabaseType.ClickHouse;
                    break;
                case "DmClientFactory":
                    databaseType = DatabaseType.Dameng;
                    break;
                case "KdbndpFactory":
                    databaseType = DatabaseType.KingbaseES;
                    break;
                case "OscarFactory":
                    databaseType = DatabaseType.ShenTong;
                    break;
                case "XGProviderFactory":
                    databaseType = DatabaseType.Xugu;
                    break;
                default:
                    databaseType = DatabaseType.Unknown;
                    break;
            }

            return databaseType ?? DatabaseType.Unknown;
        }
    }
}
