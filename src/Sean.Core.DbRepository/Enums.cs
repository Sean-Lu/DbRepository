namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Relational database type
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown,
        /// <summary>
        /// MySQL
        /// </summary>
        MySql,
        /// <summary>
        /// SQL Server
        /// </summary>
        SqlServer,
        /// <summary>
        /// SQL Server Compact Edition
        /// </summary>
        SqlServerCe,
        /// <summary>
        /// Oracle
        /// </summary>
        Oracle,
        /// <summary>
        /// SQLite
        /// </summary>
        SQLite,
        /// <summary>
        /// Access
        /// </summary>
        Access,
        /// <summary>
        /// Firebird
        /// </summary>
        Firebird,
        /// <summary>
        /// PostgreSql
        /// </summary>
        PostgreSql,
        /// <summary>
        /// DB2
        /// </summary>
        DB2,
        /// <summary>
        /// Informix
        /// </summary>
        Informix
    }

    /// <summary>
    /// SQL操作符
    /// </summary>
    public enum SqlOperation
    {
        /// <summary>
        /// 等于
        /// </summary>
        Equal,
        /// <summary>
        /// 不等于
        /// </summary>
        NotEqual,
        /// <summary>
        /// 小于
        /// </summary>
        Less,
        /// <summary>
        /// 小于等于
        /// </summary>
        LessOrEqual,
        /// <summary>
        /// 大于
        /// </summary>
        Greater,
        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterOrEqual,
        /// <summary>
        /// 包含：%xxx%
        /// </summary>
        Contains,
        /// <summary>
        /// 开始包含：xxx%
        /// </summary>
        StartsWith,
        /// <summary>
        /// 结束包含%xxx
        /// </summary>
        EndsWith,
        /// <summary>
        /// IN
        /// </summary>
        In,
    }
}
