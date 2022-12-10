using Example.Domain.Handler;
using Example.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Reflection;
using Example.Infrastructure.Impls;

namespace Example.Domain.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 领域层依赖注入
        /// </summary>
        /// <param name="services"></param>
        public static void AddDomainDI(this IServiceCollection services)
        {
            services.AddInfrastructureDI();

            services.RegisterServicesByAssemblyInterface(Assembly.GetExecutingAssembly(), "Repository", ServiceLifetime.Transient);

            #region 配置数据库和数据库提供者工厂之间的映射关系
            DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));// MySql
            DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", SqlClientFactory.Instance));// Microsoft SQL Server
            DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", OracleClientFactory.Instance));// Oracle
            DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));// SQLite
            //DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));// SQLite
            #endregion

            #region Dapper配置
            // 从数据库返回的时间字段设置默认的 DateTimeKind 属性
            Dapper.SqlMapper.AddTypeHandler<DateTime>(new DateTimeTypeHandler());
            Dapper.SqlMapper.AddTypeHandler<DateTime?>(new DateTimeNullableTypeHandler());
            #endregion

            DbFactory.OnSqlExecuting += OnSqlExecuting;
            DbFactory.OnSqlExecuted += OnSqlExecuted;

            DbFactory.JsonSerializer = NewJsonSerializer.Instance;
        }

        private static void OnSqlExecuting(SqlExecutingContext context)
        {
            //Console.WriteLine(context.SqlParameter == null
            //    ? $"######SQL准备执行: {context.Sql}"
            //    : $"######SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }

        private static void OnSqlExecuted(SqlExecutedContext context)
        {
            Console.WriteLine(context.SqlParameter == null
                ? $"######SQL已经执行: {context.Sql}"
                : $"######SQL已经执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }
    }
}
