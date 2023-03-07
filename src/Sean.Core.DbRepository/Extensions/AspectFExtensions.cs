using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Sean.Utility.AOP;

namespace Sean.Core.DbRepository.Extensions
{
    public static class AspectFExtensions
    {
        public static AspectF SqlMonitor(this AspectF aspect, ISqlMonitor sqlMonitor, IDbConnection connection, ISqlCommand sqlCommand)
        {
            return aspect.SqlMonitor(sqlMonitor, connection, sqlCommand.Sql, sqlCommand.Parameter);
        }

        public static AspectF SqlMonitor(this AspectF aspect, ISqlMonitor sqlMonitor, DbCommand command)
        {
            return aspect.SqlMonitor(sqlMonitor, command.Connection, command.CommandText, command.Parameters);
        }

        public static AspectF SqlMonitor(this AspectF aspect, ISqlMonitor sqlMonitor, IDbConnection connection, string sql, object sqlParameter)
        {
            return aspect.Combine((work) =>
            {
                var sqlExecutingContext = new SqlExecutingContext(connection, sql, sqlParameter);
                sqlMonitor?.OnSqlExecuting(sqlExecutingContext);

                var timeWatcher = new Stopwatch();
                timeWatcher.Restart();

                work();

                timeWatcher.Stop();

                var sqlExecutedContext = new SqlExecutedContext(connection, sql, sqlParameter)
                {
                    ExecutionElapsed = timeWatcher.ElapsedMilliseconds
                };
                sqlMonitor?.OnSqlExecuted(sqlExecutedContext);
            });
        }
    }
}
