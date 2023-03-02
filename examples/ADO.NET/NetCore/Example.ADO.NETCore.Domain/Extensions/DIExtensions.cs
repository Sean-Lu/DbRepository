using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Reflection;
using Example.ADO.NETCore.Infrastructure.Converter;
using Example.ADO.NETCore.Infrastructure.Extensions;
using Example.ADO.NETCore.Infrastructure.Impls;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;

namespace Example.ADO.NETCore.Domain.Extensions
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
            #endregion

            DbFactory.BulkCountLimit = 200;
            DbFactory.OnSqlExecuting += OnSqlExecuting;
            DbFactory.OnSqlExecuted += OnSqlExecuted;
            DbFactory.JsonSerializer = NewJsonSerializer.Instance;

            #endregion
        }

        private static void OnSqlExecuting(SqlExecutingContext context)
        {
            //Console.WriteLine(context.SqlParameter == null
            //    ? $"######SQL准备执行: {context.Sql}"
            //    : $"######SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
        }

        private static void OnSqlExecuted(SqlExecutedContext context)
        {
            Console.WriteLine(context.SqlParameter == null
                ? $"######SQL已经执行: {context.Sql}"
                : $"######SQL已经执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
        }
    }
}
