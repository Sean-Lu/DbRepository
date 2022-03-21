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

        public virtual string GetSqlForSelectLastInsertId()
        {
            switch (DbType)
            {
                case DatabaseType.MySql:
                    return "SELECT LAST_INSERT_ID() AS Id;";
                case DatabaseType.SqlServer:
                    // IDENT_CURRENT('TableName'): 返回为任何会话和任何作用域中的特定表最后生成的标识值
                    // SCOPE_IDENTITY(): 返回为当前会话和当前作用域中的任何表最后生成的标识值
                    // @@IDENTITY: 返回为当前会话的所有作用域中的任何表最后生成的标识值

                    //return "SELECT @@IDENTITY AS Id;";
                    return "SELECT SCOPE_IDENTITY() AS Id;";
                case DatabaseType.Oracle:
                    return "SELECT {0}.CURRVAL AS Id FROM dual;";// {0} => sequence
                case DatabaseType.SQLite:
                    return "SELECT LAST_INSERT_ROWID() AS Id;";
                case DatabaseType.Access:
                    return "SELECT @@IDENTITY AS Id;";
                case DatabaseType.PostgreSql:
                    return "SELECT LASTVAL() AS Id;";
                default:
                    throw new NotSupportedException($"[{nameof(GetSqlForSelectLastInsertId)}]-[{DbType}]");
            }
        }
    }
}
