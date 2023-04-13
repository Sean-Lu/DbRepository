﻿using System;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using Example.ADO.NETCore.Infrastructure.Converter;
using Example.ADO.NETCore.Infrastructure.Extensions;
using Example.ADO.NETCore.Infrastructure.Impls;
using FirebirdSql.Data.FirebirdClient;
using IBM.Data.DB2.Core;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Npgsql;
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
            DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("System.Data.OleDb", OleDbFactory.Instance));// MsAccess
            //DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("System.Data.Odbc", OdbcFactory.Instance));// MsAccess
            //DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("EntityFrameworkCore.Jet.Data", JetFactory.Instance.GetDataAccessProviderFactory(DataAccessProviderType.OleDb)));// MsAccess
            DatabaseType.Firebird.SetDbProviderMap(new DbProviderMap("FirebirdSql.Data.FirebirdClient", FirebirdClientFactory.Instance));// Firebird
            DatabaseType.PostgreSql.SetDbProviderMap(new DbProviderMap("Npgsql", NpgsqlFactory.Instance));// PostgreSql
            DatabaseType.DB2.SetDbProviderMap(new DbProviderMap("IBM.Data.DB2", DB2Factory.Instance));// DB2
            #endregion

            #region 创建 Firebird 数据库
            var dbPath = @".\test.fdb";
            if (!File.Exists(dbPath))
            {
                FbConnectionStringBuilder sb = new FbConnectionStringBuilder
                {
                    Database = dbPath,
                    UserID = "sysdba",
                    //Password = "masterkey",
                    Pooling = true,
                    ServerType = FbServerType.Embedded,
                    ClientLibrary = "fbclient.dll"
                };
                FbConnection.CreateDatabase(sb.ConnectionString, forcedWrites: false);
            }
            #endregion

            // 解决 PostgreSQL 在使用 DateTime 类型抛出异常：Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
                options.SetDbCommand = command =>
                {
                    if (command is OracleCommand oracleCommand)
                    {
                        // Oracle的sql参数顺序问题有以下2种解决方案：
                        // 解决方案1：把sql参数按照在sql中出现的顺序排序
                        // 解决方案2：设置 OracleCommand 按照变量名绑定参数
                        oracleCommand.BindByName = true;// default value: false
                    }
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
        }

        private static void OnSqlExecuting(SqlExecutingContext context)
        {
            //Console.WriteLine($"######SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
        }

        private static void OnSqlExecuted(SqlExecutedContext context)
        {
            if (context.Exception != null)
            {
                Console.WriteLine($"######SQL执行异常({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}{Environment.NewLine}{context.Exception}");
                return;
            }

            Console.WriteLine($"######SQL已经执行({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
        }
    }
}
