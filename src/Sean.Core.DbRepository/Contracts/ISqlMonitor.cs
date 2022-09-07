namespace Sean.Core.DbRepository
{
    public interface ISqlMonitor
    {
        void OnSqlExecuting(SqlExecutingContext context);
        void OnSqlExecuted(SqlExecutedContext context);
    }
}
