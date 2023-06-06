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
        /// <para>https://www.mysql.com/</para>
        /// </summary>
        MySql,
        /// <summary>
        /// MariaDB
        /// <para>https://mariadb.org/</para>
        /// </summary>
        MariaDB,
        /// <summary>
        /// TiDB
        /// <para>https://cn.pingcap.com/</para>
        /// </summary>
        TiDB,
        /// <summary>
        /// SQL Server
        /// <para>https://www.microsoft.com/</para>
        /// </summary>
        SqlServer,
        /// <summary>
        /// Oracle
        /// <para>https://www.oracle.com/</para>
        /// </summary>
        Oracle,
        /// <summary>
        /// SQLite
        /// <para>https://www.sqlite.org/</para>
        /// </summary>
        SQLite,
        /// <summary>
        /// DuckDB
        /// <para>https://duckdb.org/</para>
        /// </summary>
        DuckDB,
        /// <summary>
        /// MS Access
        /// <para>- MS Access 2003 (*.mdb)</para>
        /// <para>- MS Access 2007+ (*.accdb)</para>
        /// <para>https://www.microsoft.com/</para>
        /// </summary>
        MsAccess,
        /// <summary>
        /// Firebird
        /// <para>https://www.firebirdsql.org/</para>
        /// </summary>
        Firebird,
        /// <summary>
        /// PostgreSql
        /// <para>https://www.postgresql.org/</para>
        /// </summary>
        PostgreSql,
        /// <summary>
        /// DB2
        /// <para>https://www.ibm.com/</para>
        /// </summary>
        DB2,
        /// <summary>
        /// Informix
        /// <para>https://www.ibm.com/</para>
        /// </summary>
        Informix,
        /// <summary>
        /// ClickHouse
        /// <para>https://clickhouse.com/</para>
        /// </summary>
        ClickHouse,
        /// <summary>
        /// DM（达梦）
        /// <para>https://www.dameng.com/</para>
        /// </summary>
        DM,
        /// <summary>
        /// KingbaseES（人大金仓）
        /// <para>https://www.kingbase.com.cn/</para>
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
