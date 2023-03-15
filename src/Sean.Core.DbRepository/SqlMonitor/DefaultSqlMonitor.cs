using System;

namespace Sean.Core.DbRepository;

public class DefaultSqlMonitor : ISqlMonitor
{
    public event Action<SqlExecutingContext> SqlExecuting;
    public event Action<SqlExecutedContext> SqlExecuted;

    public DefaultSqlMonitor()
    {

    }

    public void OnSqlExecuting(SqlExecutingContext context)
    {
        SqlExecuting?.Invoke(context);
        if (context.Handled)
        {
            return;
        }

        DbContextConfiguration.Options.TriggerSqlExecuting(context);
    }

    public void OnSqlExecuted(SqlExecutedContext context)
    {
        SqlExecuted?.Invoke(context);
        if (context.Handled)
        {
            return;
        }

        DbContextConfiguration.Options.TriggerSqlExecuted(context);
    }
}