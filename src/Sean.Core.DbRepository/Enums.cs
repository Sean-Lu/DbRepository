namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Database type
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
        /// MariaDB
        /// </summary>
        MariaDB,
        /// <summary>
        /// TiDB
        /// </summary>
        TiDB,
        /// <summary>
        /// SQL Server
        /// </summary>
        SqlServer,
        /// <summary>
        /// Oracle
        /// </summary>
        Oracle,
        /// <summary>
        /// SQLite
        /// </summary>
        SQLite,
        /// <summary>
        /// MS Access
        /// <para>- MS Access 2003 (*.mdb)</para>
        /// <para>- MS Access 2007+ (*.accdb)</para>
        /// </summary>
        MsAccess,
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
        Informix,
        /// <summary>
        /// ClickHouse
        /// </summary>
        ClickHouse,
        /// <summary>
        /// DM（达梦）
        /// </summary>
        DM,
        /// <summary>
        /// KingbaseES（人大金仓）
        /// </summary>
        KingbaseES
    }

    public enum SqlOperation
    {
        None,
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
        In,
        NotIn,
        Like,
    }

    public enum WhereSqlKeyword
    {
        None,
        And,
        Or,
    }

    public enum Include
    {
        None,
        Left,
        Right
    }

    public enum OrderByType
    {
        Asc,
        Desc
    }

    public enum EntityStateType
    {
        Unchanged = 0,
        Added = 1,
        Modified = 2,
        Deleted = 3
    }

    public enum BindSqlParameterType
    {
        BindByName = 0,
        BindByPosition = 1
    }
}
