using System;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.Domain.Repositories;

/// <summary>
/// 通用仓储
/// </summary>
//public class CommonRepository : BaseRepository// Using ADO.NET
public class CommonRepository : DapperBaseRepository// Using Dapper
{
    private readonly ILogger _logger;

    public CommonRepository(
        IConfiguration configuration,
        ISimpleLogger<CommonRepository> logger
    ) : base(configuration, "test_SQLite")
    {
        _logger = logger;
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

/// <summary>
/// 通用仓储
/// </summary>
//public class CommonRepository<TEntity> : BaseRepository<TEntity> where TEntity : class// Using ADO.NET
public class CommonRepository<TEntity> : DapperBaseRepository<TEntity> where TEntity : class// Using Dapper
{
    private readonly ILogger _logger;

    public CommonRepository(
        IConfiguration configuration,
        ISimpleLogger<CommonRepository<TEntity>> logger
    ) : base(configuration, "test_SQLite")
    {
        _logger = logger;
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