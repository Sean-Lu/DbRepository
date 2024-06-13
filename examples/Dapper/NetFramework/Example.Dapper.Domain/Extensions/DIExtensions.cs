using System;
using System.IO;
using System.Reflection;
using Example.Dapper.Domain.Handler;
using Example.Dapper.Domain.Repositories;
using Example.Dapper.Infrastructure.Extensions;
using Example.Dapper.Infrastructure.Impls;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DependencyInjection;

namespace Example.Dapper.Domain.Extensions
{
    public static class DIExtensions
    {
        /// <summary>
        /// 领域层依赖注入
        /// </summary>
        /// <param name="container"></param>
        public static void AddDomainDI(this IDIRegister container)
        {
            container.AddInfrastructureDI();

            container.RegisterAssemblyByInterfaceSuffix(Assembly.GetExecutingAssembly(), "Repository", ServiceLifeStyle.Transient);

            container.RegisterType(typeof(CommonRepository), typeof(CommonRepository), ServiceLifeStyle.Transient);// 注册通用仓储
            container.RegisterType(typeof(CommonRepository<>), typeof(CommonRepository<>), ServiceLifeStyle.Transient);// 注册通用仓储（泛型）

            #region Database configuration.

            #region 配置数据库和数据库提供者工厂之间的映射关系
            //DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance));// MySql
            //DatabaseType.MariaDB.SetDbProviderMap(new DbProviderMap("MySqlConnector.MariaDB", MySqlConnector.MySqlConnectorFactory.Instance));// MariaDB
            //DatabaseType.TiDB.SetDbProviderMap(new DbProviderMap("TiDB", MySql.Data.MySqlClient.MySqlClientFactory.Instance));// TiDB
            //DatabaseType.OceanBase.SetDbProviderMap(new DbProviderMap("OceanBase", MySql.Data.MySqlClient.MySqlClientFactory.Instance));// OceanBase
            //DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", System.Data.SqlClient.SqlClientFactory.Instance));// Microsoft SQL Server
            //DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance));// Oracle
            //DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", System.Data.SQLite.SQLiteFactory.Instance));// SQLite
            //DatabaseType.DuckDB.SetDbProviderMap(new DbProviderMap("DuckDB.NET.Data", DuckDB.NET.Data.DuckDBClientFactory.Instance));// DuckDB
            //DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("System.Data.OleDb", System.Data.OleDb.OleDbFactory.Instance));// MsAccess
            ////DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("System.Data.Odbc", System.Data.Odbc.OdbcFactory.Instance));// MsAccess
            ////DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("EntityFrameworkCore.Jet.Data", JetFactory.Instance.GetDataAccessProviderFactory(DataAccessProviderType.OleDb)));// MsAccess
            //DatabaseType.Firebird.SetDbProviderMap(new DbProviderMap("FirebirdSql.Data.FirebirdClient", FirebirdSql.Data.FirebirdClient.FirebirdClientFactory.Instance));// Firebird
            //DatabaseType.PostgreSql.SetDbProviderMap(new DbProviderMap("Npgsql", Npgsql.NpgsqlFactory.Instance));// PostgreSql
            //DatabaseType.OpenGauss.SetDbProviderMap(new DbProviderMap("OpenGauss", OpenGauss.NET.OpenGaussFactory.Instance));// OpenGauss
            //DatabaseType.HighgoDB.SetDbProviderMap(new DbProviderMap("HighgoDB", Npgsql.NpgsqlFactory.Instance));// HighgoDB
            //DatabaseType.IvorySQL.SetDbProviderMap(new DbProviderMap("IvorySQL", Npgsql.NpgsqlFactory.Instance));// IvorySQL
            //DatabaseType.QuestDB.SetDbProviderMap(new DbProviderMap("QuestDB", Npgsql.NpgsqlFactory.Instance));// QuestDB
            //DatabaseType.DB2.SetDbProviderMap(new DbProviderMap("IBM.Data.DB2", IBM.Data.DB2.Core.DB2Factory.Instance));// DB2
            //DatabaseType.Informix.SetDbProviderMap(new DbProviderMap("IBM.Data.Informix", IBM.Data.Informix.IfxFactory.Instance));// Informix
            //DatabaseType.ClickHouse.SetDbProviderMap(new DbProviderMap("ClickHouse.Client", new ClickHouse.Client.ADO.ClickHouseConnectionFactory()));// ClickHouse
            //DatabaseType.Dameng.SetDbProviderMap(new DbProviderMap("Dameng", Dm.DmClientFactory.Instance));// Dameng（达梦）
            //DatabaseType.KingbaseES.SetDbProviderMap(new DbProviderMap("Kdbndp", Kdbndp.KdbndpFactory.Instance));// KingbaseES（人大金仓）
            //DatabaseType.ShenTong.SetDbProviderMap(new DbProviderMap("ShenTong", System.Data.OscarClient.OscarFactory.Instance));// ShenTong（神通数据库）
            //DatabaseType.Xugu.SetDbProviderMap(new DbProviderMap("Xugu", new XuguClient.XGProviderFactory(null)));// Xugu（虚谷数据库）

            DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"));// MySql
            DatabaseType.MariaDB.SetDbProviderMap(new DbProviderMap("MySqlConnector.MariaDB", "MySqlConnector.MySqlConnectorFactory,MySqlConnector"));// MariaDB
            DatabaseType.TiDB.SetDbProviderMap(new DbProviderMap("TiDB", "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"));// TiDB
            DatabaseType.OceanBase.SetDbProviderMap(new DbProviderMap("OceanBase", "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"));// OceanBase
            DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", "System.Data.SqlClient.SqlClientFactory,System.Data.SqlClient"));// Microsoft SQL Server
            DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", "Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess"));// Oracle
            DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));// SQLite
            DatabaseType.DuckDB.SetDbProviderMap(new DbProviderMap("DuckDB.NET.Data", "DuckDB.NET.Data.DuckDBClientFactory,DuckDB.NET.Data"));// DuckDB
            DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("System.Data.OleDb", "System.Data.OleDb.OleDbFactory,System.Data"));// MsAccess
            //DatabaseType.MsAccess.SetDbProviderMap(new DbProviderMap("System.Data.Odbc", "System.Data.Odbc.OdbcFactory,System.Data"));// MsAccess
            DatabaseType.Firebird.SetDbProviderMap(new DbProviderMap("FirebirdSql.Data.FirebirdClient", "FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,FirebirdSql.Data.FirebirdClient"));// Firebird
            DatabaseType.PostgreSql.SetDbProviderMap(new DbProviderMap("Npgsql", "Npgsql.NpgsqlFactory,Npgsql"));// PostgreSql
            DatabaseType.OpenGauss.SetDbProviderMap(new DbProviderMap("OpenGauss", "OpenGauss.NET.OpenGaussFactory,OpenGauss.NET"));// OpenGauss
            DatabaseType.HighgoDB.SetDbProviderMap(new DbProviderMap("HighgoDB", "Npgsql.NpgsqlFactory,Npgsql"));// HighgoDB
            DatabaseType.IvorySQL.SetDbProviderMap(new DbProviderMap("IvorySQL", "Npgsql.NpgsqlFactory,Npgsql"));// IvorySQL
            DatabaseType.QuestDB.SetDbProviderMap(new DbProviderMap("QuestDB", "Npgsql.NpgsqlFactory,Npgsql"));// QuestDB
            DatabaseType.DB2.SetDbProviderMap(new DbProviderMap("IBM.Data.DB2", "IBM.Data.DB2.Core.DB2Factory,IBM.Data.DB2.Core"));// DB2
            DatabaseType.Informix.SetDbProviderMap(new DbProviderMap("IBM.Data.Informix", "IBM.Data.Informix.IfxFactory,IBM.Data.Informix"));// Informix
            DatabaseType.ClickHouse.SetDbProviderMap(new DbProviderMap("ClickHouse.Client", "ClickHouse.Client.ADO.ClickHouseConnectionFactory,ClickHouse.Client"));// ClickHouse
            DatabaseType.Dameng.SetDbProviderMap(new DbProviderMap("Dameng", "Dm.DmClientFactory,DmProvider"));// Dameng（达梦）
            DatabaseType.KingbaseES.SetDbProviderMap(new DbProviderMap("Kdbndp", "Kdbndp.KdbndpFactory,Kdbndp"));// KingbaseES（人大金仓）
            DatabaseType.ShenTong.SetDbProviderMap(new DbProviderMap("ShenTong", "System.Data.OscarClient.OscarFactory,Oscar.Data.SqlClient"));// ShenTong（神通数据库）
            DatabaseType.Xugu.SetDbProviderMap(new DbProviderMap("Xugu", "XuguClient.XGProviderFactory,XuguClient"));// Xugu（虚谷数据库）
            #endregion

#if UseFirebird
            #region 自动创建 Firebird 数据库
            FirebirdSql.Data.FirebirdClient.FbConnectionStringBuilder sb = new FirebirdSql.Data.FirebirdClient.FbConnectionStringBuilder
            {
                Database = @".\test.fdb",
                UserID = "sysdba",
                //Password = "masterkey",
                Pooling = true,
                ServerType = FirebirdSql.Data.FirebirdClient.FbServerType.Embedded,
                ClientLibrary = "fbclient.dll"
            };
            if (!File.Exists(sb.Database) && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sb.ClientLibrary)))
            {
                FirebirdSql.Data.FirebirdClient.FbConnection.CreateDatabase(sb.ConnectionString);
            }
            #endregion
#endif

#if UsePostgreSql || UseHighgoDB || UseIvorySQL || UseQuestDB
            // 解决 PostgreSQL 在使用 DateTime 类型抛出异常：Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#endif

#if UseOpenGauss
            // 解决 OpenGauss 在使用 DateTime 类型抛出异常：Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
            AppContext.SetSwitch("OpenGauss.NET.EnableLegacyTimestampBehavior", true);
#endif

            DbContextConfiguration.Configure(options =>
            {
                //options.DefaultNamingConvention = NamingConvention.SnakeCase;
#if UseSqlite
                options.SynchronousWriteOptions.Enable = true;// 启用同步写入模式：解决多线程并发写入导致的锁库问题
                options.SynchronousWriteOptions.LockTimeout = 5000;// 同步写入锁等待超时时间（单位：毫秒），默认值：5000
#endif
                options.MapToDatabaseType = factory =>
                {
                    return factory switch
                    {
#if UseMsAccess
                        System.Data.OleDb.OleDbFactory or System.Data.Odbc.OdbcFactory => DatabaseType.MsAccess,
#endif
                        _ => DatabaseType.Unknown
                    };
                };
                options.SetDbCommand = command =>
                {
#if UseOracle
                    if (command is Oracle.ManagedDataAccess.Client.OracleCommand oracleCommand)
                    {
                        // Oracle的sql参数顺序问题有以下2种解决方案：
                        // 解决方案1：把sql参数按照在sql中出现的顺序排序
                        // 解决方案2：设置 OracleCommand 按照变量名绑定参数
                        oracleCommand.BindByName = true;// default value: false
                    }
#endif
                };
                options.BulkEntityCount = 200;
                options.JsonSerializer = NewJsonSerializer.Instance;
                options.SqlExecuting += OnSqlExecuting;
                options.SqlExecuted += OnSqlExecuted;
                //options.SqlParameterized = false;
            });

#if UseSqlServer
            //DbContextConfiguration.ConfigureSqlServer(options =>
            //{
            //    options.UseRowNumberForPaging = true;// SQL Server 2005 ~ 2008
            //});
#endif

            #endregion

            #region Dapper配置
            //// 从数据库返回的 DateTime 设置默认的 DateTimeKind 属性
            //global::Dapper.SqlMapper.AddTypeHandler<DateTime>(new DateTimeTypeHandler());
            //global::Dapper.SqlMapper.AddTypeHandler<DateTime?>(new DateTimeNullableTypeHandler());

#if UseMsAccess
            // 解决 MS Access 数据库实体类使用 DateTime 类型的属性会报错的问题【OleDbException: 标准表达式中数据类型不匹配。】
            global::Dapper.SqlMapper.RemoveTypeMap(typeof(DateTime));
            global::Dapper.SqlMapper.AddTypeHandler<DateTime>(new MsAccessDateTimeHandler());
            global::Dapper.SqlMapper.RemoveTypeMap(typeof(DateTime?));
            global::Dapper.SqlMapper.AddTypeHandler<DateTime?>(new MsAccessDateTimeNullableHandler());
#endif

#if UseOracle
            // 解决 Oracle 数据库实体类使用 bool 类型的属性会报错的问题【ORA-03115: 不支持的网络数据类型或表示法】
            global::Dapper.SqlMapper.RemoveTypeMap(typeof(bool));
            global::Dapper.SqlMapper.AddTypeHandler<bool>(new OracleBoolTypeHandler());
#endif

#if UseDuckDB || UseXugu
            // 解决 DuckDB 数据库实体类使用 decimal 类型的属性会报错的问题
            global::Dapper.SqlMapper.RemoveTypeMap(typeof(decimal));
            global::Dapper.SqlMapper.AddTypeHandler<decimal>(new DuckDBDecimalTypeHandler());
#endif
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
