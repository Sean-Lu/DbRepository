using System.Data;

namespace Sean.Core.DbRepository;

public class SqlExecutedContext
{
    public SqlExecutedContext(IDbConnection connection, string sql, object sqlParameter)
    {
        Connection = connection;
        Sql = sql;
        SqlParameter = sqlParameter;
    }

    public IDbConnection Connection { get; }
    public string Sql { get; }
    public object SqlParameter { get; }
}