using System;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Reflection;
using Example.Dapper.Core.Domain.Handler;
using Example.Dapper.Core.Infrastructure.Extensions;
using Example.Dapper.Core.Infrastructure.Impls;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;

namespace Example.Dapper.Core.Domain.Extensions
{
    public static class DIExtensions
    {
        /// <summary>
        /// 领域层依赖注入
        /// </summary>
        /// <param name="services"></param>
        public static void AddDomainDI(this IServiceCollection services)
        {
            services.AddInfrastructureDI();

            services.RegisterByAssemblyInterface(Assembly.GetExecutingAssembly(), "Repository", ServiceLifetime.Transient);

            #region Database configuration.

            #region 配置数据库和数据库提供者工厂之间的映射关系
            DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));// MySql
            DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", SqlClientFactory.Instance));// Microsoft SQL Server
            DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", OracleClientFactory.Instance));// Oracle
            DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));// SQLite
            //DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));// SQLite
            DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("System.Data.OleDb", OleDbFactory.Instance));// MsAccess
            //DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("EntityFrameworkCore.Jet.Data", JetFactory.Instance.GetDataAccessProviderFactory(DataAccessProviderType.OleDb)));// MsAccess
            #endregion

            DbContextConfiguration.Configure(options =>
            {
                options.MapToDatabaseType = factory =>
                {
                    if (factory is OleDbFactory or OdbcFactory)
                    {
                        return DatabaseType.MsAccess;
                    }
                    return DatabaseType.Unknown;
                };
                options.BulkEntityCount = 200;
                options.JsonSerializer = NewJsonSerializer.Instance;
                options.SqlExecuting += OnSqlExecuting;
                options.SqlExecuted += OnSqlExecuted;
            });

            //DbContextConfiguration.ConfigureSqlServer(options =>
            //{
            //    options.UseRowNumberForPaging = true;// SQL Server 2005 ~ 2008
            //});

            #endregion

            #region Dapper配置
            // 从数据库返回的时间字段设置默认的 DateTimeKind 属性
            global::Dapper.SqlMapper.AddTypeHandler<DateTime>(new DateTimeTypeHandler());
            global::Dapper.SqlMapper.AddTypeHandler<DateTime?>(new DateTimeNullableTypeHandler());
            #endregion
        }

        private static void OnSqlExecuting(SqlExecutingContext context)
        {
            //Console.WriteLine($"######SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }

        private static void OnSqlExecuted(SqlExecutedContext context)
        {
            if (context.Exception != null)
            {
                Console.WriteLine($"######SQL执行异常({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}{Environment.NewLine}{context.Exception}");
                return;
            }

            Console.WriteLine($"######SQL已经执行({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }
    }
}
