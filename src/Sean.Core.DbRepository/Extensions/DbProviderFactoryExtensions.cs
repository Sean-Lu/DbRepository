using System.Data.Common;

namespace Sean.Core.DbRepository.Extensions
{
    public static class DbProviderFactoryExtensions
    {
        public static DatabaseType GetDatabaseType(this DbProviderFactory providerFactory)
        {
            if (providerFactory == null)
            {
                return DatabaseType.Unknown;
            }

            var dbType = DatabaseType.Unknown;
            var name = providerFactory.GetType().Name;//Connecttion.GetType()
            if (name.StartsWith("MySql")) dbType = DatabaseType.MySql;
            else if (name.StartsWith("SqlClient")) dbType = DatabaseType.SqlServer;
            else if (name.StartsWith("Oracle")) dbType = DatabaseType.Oracle;
            else if (name.StartsWith("SQLite")) dbType = DatabaseType.SQLite;
            //else if (dbFactoryName.StartsWith("OleDb")) _dbType = DatabaseType.Access;
            else if (name.StartsWith("Firebird")) dbType = DatabaseType.Firebird;
            else if (name.StartsWith("Npgsql")) dbType = DatabaseType.PostgreSql;
            else if (name.StartsWith("DB2")) dbType = DatabaseType.DB2;
            else if (name.StartsWith("Ifx")) dbType = DatabaseType.Informix;
            return dbType;
        }
    }
}
