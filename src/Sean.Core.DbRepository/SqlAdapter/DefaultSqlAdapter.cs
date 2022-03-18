using System;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public class DefaultSqlAdapter : ISqlAdapter
    {
        public DatabaseType DbType { get; }

        public DefaultSqlAdapter(DatabaseType dbType)
        {
            DbType = dbType;
        }

        public virtual string FormatTableName(string tableName)
        {
            return DbType.MarkAsTableOrFieldName(tableName);
        }

        public virtual string FormatFieldName(string fieldName)
        {
            return DbType.MarkAsTableOrFieldName(fieldName);
        }

        public virtual string FormatInputParameter(string parameter)
        {
            return DbType.MarkAsSqlInputParameter(parameter);
        }

        /// <summary>
        /// SQL语句：获取上一次插入id
        /// </summary>
        /// <returns></returns>
        public virtual string GetSqlForSelectLastInsertId()
        {
            switch (DbType)
            {
                case DatabaseType.MySql:
                    return "SELECT LAST_INSERT_ID();";
                case DatabaseType.SqlServer:
                case DatabaseType.Access:
                    return "SELECT @@Identity;";
                case DatabaseType.SQLite:
                    return "SELECT last_insert_rowid();";
                case DatabaseType.PostgreSql:
                    return "SELECT LASTVAL();";
                case DatabaseType.Oracle:
                    return "SELECT {0}.CURRVAL FROM dual;";// {0} => sequence
                default:
                    throw new NotSupportedException($"[{nameof(GetSqlForSelectLastInsertId)}]-[{DbType}]");
            }
        }
    }
}
