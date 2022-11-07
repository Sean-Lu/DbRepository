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

            #region 配置数据库驱动映射关系
            // 方式1：在配置文件中配置映射关系
            // 方式2：在代码里直接指定映射关系
            DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));
            DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", SqlClientFactory.Instance));
            DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", OracleClientFactory.Instance));
            DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));
            #endregion

            #region Dapper配置
            // 从数据库返回的时间字段指定DateTimeKind
            Dapper.SqlMapper.AddTypeHandler<DateTime>(new DateTimeTypeHandler());
            Dapper.SqlMapper.AddTypeHandler<DateTime?>(new DateTimeNullableTypeHandler());
            #endregion

            DbFactory.OnSqlExecuting += OnSqlExecuting;
            DbFactory.OnSqlExecuted += OnSqlExecuted;

            DbFactory.JsonSerializer = NewJsonSerializer.Instance;
        }

        private static void OnSqlExecuting(SqlExecutingContext context)
        {
            //Console.WriteLine($"######SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }

        private static void OnSqlExecuted(SqlExecutedContext context)
        {
            Console.WriteLine($"######SQL已经执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        }
    }
}
