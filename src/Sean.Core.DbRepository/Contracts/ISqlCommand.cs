using System.Data;

namespace Sean.Core.DbRepository;

public interface ISqlCommand
{
    /// <summary>
    /// Command text
    /// </summary>
    string Sql { get; set; }
    /// <summary>
    /// The input parameter of the SQL statement or stored procedure.
    /// </summary>
    object Parameter { get; set; }
    /// <summary>
    /// true: master database, false: slave database.
    /// </summary>
    bool Master { get; set; }
    /// <summary>
    /// Database transaction.
    /// </summary>
    IDbTransaction Transaction { get; set; }
    /// <summary>
    /// Database connection.
    /// </summary>
    IDbConnection Connection { get; set; }
    /// <summary>
    /// The time (in seconds) to wait for the command to execute. The default value is 30 seconds.
    /// </summary>
    int? CommandTimeout { get; set; }
    /// <summary>
    /// Command type
    /// </summary>
    CommandType CommandType { get; set; }
}

public interface ISqlCommand<T> : ISqlCommand
{
    new T Parameter { get; set; }
}