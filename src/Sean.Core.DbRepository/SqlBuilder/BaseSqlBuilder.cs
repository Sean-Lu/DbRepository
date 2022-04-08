using System;

namespace Sean.Core.DbRepository
{
    public abstract class BaseSqlBuilder
    {
        /// <summary>
        /// SQL适配器
        /// </summary>
        public ISqlAdapter SqlAdapter { get; }

        protected BaseSqlBuilder(DatabaseType dbType, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

            SqlAdapter = new DefaultSqlAdapter(dbType, tableName);
        }
    }
}