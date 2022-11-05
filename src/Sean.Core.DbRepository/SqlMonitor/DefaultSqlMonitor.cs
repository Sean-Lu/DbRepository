using System;

namespace Sean.Core.DbRepository
{
    public class DefaultSqlMonitor : ISqlMonitor
    {
        private readonly Action<SqlExecutingContext> _sqlExecuting;
        private readonly Action<SqlExecutedContext> _aqlExecuted;

        public DefaultSqlMonitor(Action<SqlExecutingContext> sqlExecuting, Action<SqlExecutedContext> aqlExecuted)
        {
            _sqlExecuting = sqlExecuting;
            _aqlExecuted = aqlExecuted;
        }

        public void OnSqlExecuting(SqlExecutingContext context)
        {
            _sqlExecuting?.Invoke(context);
        }

        public void OnSqlExecuted(SqlExecutedContext context)
        {
            _aqlExecuted?.Invoke(context);
        }
    }
}
