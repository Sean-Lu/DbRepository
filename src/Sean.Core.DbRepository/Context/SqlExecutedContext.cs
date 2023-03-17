using System;
using System.Data;

namespace Sean.Core.DbRepository;

public class SqlExecutedContext
{
    public SqlExecutedContext(IDbConnection connection, string sql, object sqlParameter = null)
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
    /// <summary>
    /// Gets the execution elapsed time, in milliseconds.
    /// </summary>
    public long ExecutionElapsed { get; set; }

    public Exception Exception { get; set; }
}