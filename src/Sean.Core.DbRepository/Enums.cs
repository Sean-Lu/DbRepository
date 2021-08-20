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

    /// <summary>
    /// WHERE SQL语句关键字：AND、OR
    /// </summary>
    public enum WhereSqlKeyword
    {
        None,
        /// <summary>
        /// AND
        /// </summary>
        And,
        /// <summary>
        /// OR
        /// </summary>
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

    /// <summary>
    /// ORDER BY 排序方式：ASC、DESC
    /// </summary>
    public enum OrderByType
    {
        /// <summary>
        /// ASC：升序（默认排序方式）
        /// </summary>
        Asc,
        /// <summary>
        /// DESC：降序
        /// </summary>
        Desc
    }

    /// <summary>
    /// SQL关键字
    /// </summary>
    public enum SqlKeyword
    {
        None,
        Create,
        Drop,
        Index,
        /// <summary>
        /// INSERT INTO
        /// </summary>
        InsertInto,
        /// <summary>
        /// DELETE
        /// </summary>
        Delete,
        /// <summary>
        /// UPDATE
        /// </summary>
        Update,
        /// <summary>
        /// SELECT
        /// </summary>
        Select,
        /// <summary>
        /// FROM
        /// </summary>
        From,
        Top,
        Limit,
        Between,
        Union,
        UnionAll,
        Distinct,
        Where,
        GroupBy,
        Having,
        OrderBy,
        /// <summary>
        /// IN
        /// </summary>
        In,
        /// <summary>
        /// LIKE
        /// </summary>
        Like,
        /// <summary>
        /// AND
        /// </summary>
        And,
        /// <summary>
        /// OR
        /// </summary>
        Or,
        /// <summary>
        /// INNER JOIN（同JOIN）：如果表中有至少一个匹配，则返回行
        /// </summary>
        InnerJoin,
        /// <summary>
        /// LEFT JOIN：即使右表中没有匹配，也从左表返回所有的行
        /// </summary>
        LeftJoin,
        /// <summary>
        /// RIGHT JOIN：即使左表中没有匹配，也从右表返回所有的行
        /// </summary>
        RightJoin,
        /// <summary>
        /// FULL JOIN：只要其中一个表中存在匹配，则返回行
        /// </summary>
        FullJoin,
    }
}
