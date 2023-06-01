using System;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Utility.Contracts;

namespace Example.Dapper.Domain.Repositories
{
    /// <summary>
    /// 通用仓储
    /// </summary>
    public class CommonRepository<TEntity> : BaseRepository<TEntity> where TEntity : class
    {
        private readonly ILogger _logger;

        public CommonRepository(
            ILogger<CommonRepository<TEntity>> logger
            //) : base()// MySQL: CRUD test passed.
            //) : base("test_MariaDB")// MariaDB: CRUD test passed.
            //) : base("test_SqlServer")// SQL Server: CRUD test passed.
            //) : base("test_Oracle")// Oracle: CRUD test passed.
            ) : base("test_SQLite")// SQLite: CRUD test passed.
            //) : base("test_MsAccess")// MS Access: CRUD test passed.
            //) : base("test_Firebird")// Firebird: CRUD test passed.
            //) : base("test_PostgreSql")// PostgreSql: CRUD test passed.
            //) : base("test_DB2")// DB2: CRUD test passed.
            //) : base("test_Informix")// Informix: CRUD test passed.
            //) : base("test_ClickHouse")// ClickHouse: CRUD test passed.
            //) : base("test_DM")// DM（达梦）: CRUD test passed.
            //) : base("test_KingbaseES")// KingbaseES（人大金仓）: CRUD test passed.
        {
            _logger = logger;

            //DbType = DatabaseType.MariaDB;
        }

        protected override void OnSqlExecuting(SqlExecutingContext context)
        {
            base.OnSqlExecuting(context);

            //_logger.LogInfo($"SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
            //context.Handled = true;
        }

        protected override void OnSqlExecuted(SqlExecutedContext context)
        {
            base.OnSqlExecuted(context);

            if (context.Exception != null)
            {
                _logger.LogError($"SQL执行异常({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}{Environment.NewLine}{context.Exception}");
                context.Handled = true;
                return;
            }

            _logger.LogInfo($"SQL已经执行({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
            context.Handled = true;
        }
    }
}
