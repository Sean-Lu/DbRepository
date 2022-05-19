using System;

namespace Sean.Core.DbRepository
{
    public abstract class BaseSqlBuilder
    {
        /// <summary>
        /// SQL是否缩进格式化处理，默认值：false
        /// </summary>
        public static bool SqlIndented = false;
        /// <summary>
        /// SQL是否参数化处理，默认值：true
        /// </summary>
        public static bool SqlParameterized = true;

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