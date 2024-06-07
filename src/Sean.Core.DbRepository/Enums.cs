namespace Sean.Core.DbRepository;

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
    /// <para>https://github.com/mysql/mysql-server</para>
    /// </summary>
    MySql,
    /// <summary>
    /// MariaDB
    /// <para>https://mariadb.org/</para>
    /// <para>https://github.com/MariaDB/server</para>
    /// </summary>
    MariaDB,
    /// <summary>
    /// TiDB
    /// <para>https://pingcap.com/</para>
    /// <para>https://github.com/pingcap/tidb</para>
    /// </summary>
    TiDB,
    /// <summary>
    /// OceanBase
    /// <para>https://open.oceanbase.com/</para>
    /// <para>https://github.com/oceanbase/oceanbase</para>
    /// </summary>
    OceanBase,
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
    /// <para>https://github.com/sqlite/sqlite</para>
    /// </summary>
    SQLite,
    /// <summary>
    /// DuckDB
    /// <para>https://duckdb.org/</para>
    /// <para>https://github.com/duckdb/duckdb</para>
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
    /// <para>https://github.com/FirebirdSQL/firebird</para>
    /// </summary>
    Firebird,
    /// <summary>
    /// PostgreSql
    /// <para>https://www.postgresql.org/</para>
    /// <para>https://github.com/postgres/postgres</para>
    /// </summary>
    PostgreSql,
    /// <summary>
    /// OpenGauss
    /// <para>https://opengauss.org/</para>
    /// <para>https://github.com/opengauss-mirror/openGauss-server</para>
    /// </summary>
    OpenGauss,
    /// <summary>
    /// HighgoDB（瀚高数据库）
    /// <para>https://www.highgo.com/</para>
    /// </summary>
    HighgoDB,
    /// <summary>
    /// IvorySQL
    /// <para>https://ivorysql.org/</para>
    /// <para>https://github.com/IvorySQL/IvorySQL</para>
    /// </summary>
    IvorySQL,
    /// <summary>
    /// QuestDB
    /// <para>https://questdb.io/</para>
    /// <para>https://github.com/questdb/questdb</para>
    /// </summary>
    QuestDB,
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
    /// Dameng（达梦）
    /// <para>https://www.dameng.com/</para>
    /// </summary>
    Dameng,
    /// <summary>
    /// KingbaseES（人大金仓）
    /// <para>https://www.kingbase.com.cn/</para>
    /// </summary>
    KingbaseES,
    /// <summary>
    /// ShenTong（神通数据库）
    /// <para>http://www.shentongdata.com/</para>
    /// </summary>
    ShenTong,
    /// <summary>
    /// Xugu（虚谷数据库）
    /// <para>http://www.xugucn.com/</para>
    /// <para>https://gitee.com/XuguDB</para>
    /// </summary>
    Xugu
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
    Unchanged,
    Added,
    Modified,
    Deleted
}

public enum BindSqlParameterType
{
    BindByName,
    BindByPosition
}

/// <summary>
/// 命名规范
/// </summary>
public enum NamingConvention
{
    Default,
    /// <summary>
    /// 帕斯卡命名法（Pascal Case）。示例：UserName、ProductPrice、IsValid
    /// </summary>
    PascalCase,
    /// <summary>
    /// 驼峰命名法（Camel Case）。示例：userName、productPrice、isValid
    /// </summary>
    CamelCase,
    /// <summary>
    /// 蛇形命名法（Snake Case）。示例：user_name、product_price、is_valid
    /// </summary>
    SnakeCase,
    /// <summary>
    /// 大写蛇形命名法（Upper Snake Case）。示例：USER_NAME、PRODUCT_PRICE、IS_VALID
    /// </summary>
    UpperSnakeCase
}