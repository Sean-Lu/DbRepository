using System;

namespace Sean.Core.DbRepository
{
    public class DefaultSqlMonitor : ISqlMonitor
    {
        public event Action<SqlExecutingContext> SqlExecuting;
        public event Action<SqlExecutedContext> SqlExecuted;

        private readonly Action<SqlExecutingContext> _sqlExecuting;
        private readonly Action<SqlExecutedContext> _aqlExecuted;

        public DefaultSqlMonitor()
        {

        }
        public DefaultSqlMonitor(Action<SqlExecutingContext> sqlExecuting, Action<SqlExecutedContext> aqlExecuted)
        {
            _sqlExecuting = sqlExecuting;
            _aqlExecuted = aqlExecuted;
        }

        public void OnSqlExecuting(SqlExecutingContext context)
        {
            SqlExecuting?.Invoke(context);
            if (context.Handled)
            {
                return;
            }
            _sqlExecuting?.Invoke(context);
        }

        public void OnSqlExecuted(SqlExecutedContext context)
        {
            SqlExecuted?.Invoke(context);
            if (context.Handled)
            {
                return;
            }
            _aqlExecuted?.Invoke(context);
        }
    }
}
