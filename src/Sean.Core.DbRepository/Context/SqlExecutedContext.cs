using System;
using System.Data;

namespace Sean.Core.DbRepository;

public class SqlExecutedContext
{
    public SqlExecutedContext(IDbConnection connection, IDbTransaction transaction, string sql, object sqlParameter = null)
    {
        Connection = connection;
        Transaction = transaction;
        Sql = sql;
        SqlParameter = sqlParameter;
    }

    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }
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