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
        ClickHouse
    }

    public enum SqlOperation
    {
        None,
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
        /// IN
        /// </summary>
        In,
        /// <summary>
        /// NOT IN
        /// </summary>
        NotIn,
        /// <summary>
        /// LIKE
        /// </summary>
        Like,
    }

    public enum WhereSqlKeyword
    {
        None,
        And,
        Or,
    }

    /// <summary>
    /// 括号
    /// </summary>
    public enum Include
    {
        None,
        /// <summary>
        /// 左括号：(
        /// </summary>
        Left,
        /// <summary>
        /// 右括号：)
        /// </summary>
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
