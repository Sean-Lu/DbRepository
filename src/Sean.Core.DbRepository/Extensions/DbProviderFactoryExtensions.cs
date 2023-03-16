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
            if (factoryName.StartsWith("MySql")) databaseType = DatabaseType.MySql;
            else if (factoryName.StartsWith("SqlClient")) databaseType = DatabaseType.SqlServer;
            else if (factoryName.StartsWith("Oracle")) databaseType = DatabaseType.Oracle;
            else if (factoryName.StartsWith("SQLite")) databaseType = DatabaseType.SQLite;
            //else if (factoryName.StartsWith("OleDb")) databaseType = DatabaseType.MsAccess;
            else if (factoryName.StartsWith("Firebird")) databaseType = DatabaseType.Firebird;
            else if (factoryName.StartsWith("Npgsql")) databaseType = DatabaseType.PostgreSql;
            else if (factoryName.StartsWith("DB2")) databaseType = DatabaseType.DB2;
            else if (factoryName.StartsWith("Ifx")) databaseType = DatabaseType.Informix;

            return databaseType ?? DatabaseType.Unknown;
        }
    }
}
