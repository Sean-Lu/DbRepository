using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Util
{
    internal static class SqlUtil
    {
        public static string GetSqlForCountTable(DatabaseType databaseType, string database, string tableName)
        {
            return databaseType.GetSqlForCountTable(database, tableName);
        }

        public static string GetSqlForCountTableField(DatabaseType databaseType, string database, string tableName, string fieldName)
        {
            return databaseType.GetSqlForCountTableField(database, tableName, fieldName);
        }
    }
}
