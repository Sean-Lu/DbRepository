using System;

namespace Sean.Core.DbRepository;

public interface ISqlMonitor
{
    event Action<SqlExecutingContext> SqlExecuting;
    event Action<SqlExecutedContext> SqlExecuted;

    void OnSqlExecuting(SqlExecutingContext context);
    void OnSqlExecuted(SqlExecutedContext context);
}