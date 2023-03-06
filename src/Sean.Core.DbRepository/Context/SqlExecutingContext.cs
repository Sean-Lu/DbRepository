using System.Data;

namespace Sean.Core.DbRepository;

public class SqlExecutingContext
{
    public SqlExecutingContext(IDbConnection connection, string sql, object sqlParameter = null)
    {
        Connection = connection;
        Sql = sql;
        SqlParameter = sqlParameter;
    }

    public IDbConnection Connection { get; }
    public string Sql { get; }
    public object SqlParameter { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the event was handled.
    /// </summary>
    public bool Handled { get; set; }
    //public bool Cancel { get; set; }
}