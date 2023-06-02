using System.Diagnostics;
using Example.EF.Core.ConsoleApp.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

#if UseSqlServer
using EntityFrameworkCore.UseRowNumberForPaging;
#endif

#if UseMsAccess
using EntityFrameworkCore.Jet.Data;
#endif

#if UseFirebird
using FirebirdSql.Data.FirebirdClient;
#endif

#if UseDB2 || UseInformix
using IBM.Data.Db2;
using IBM.EntityFrameworkCore;
#endif

#if UseClickHouse
using ClickHouse.Client.ADO;
using ClickHouse.EntityFrameworkCore.Extensions;
#endif

namespace Example.EF.Core.ConsoleApp
{
    public class EFDbContext : DbContext
    {
        //private static readonly ILoggerFactory _LoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

        public EFDbContext()
        {
        }

        public EFDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            UseSqlite(optionsBuilder);

            //// 设置不跟踪所有查询
            //optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            // 启用敏感数据日志记录
            optionsBuilder.EnableSensitiveDataLogging();

            //optionsBuilder.UseLoggerFactory(_LoggerFactory);

            // 记录日志，包含生成的sql
            optionsBuilder.LogTo(msg =>
            {
                Debug.WriteLine(msg);// 输出调试窗口消息
                Console.WriteLine(msg);// 输出控制台窗口消息
            }, LogLevel.Information);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<TestEntity>();

            //modelBuilder.Entity<TestEntity>(c =>
            //{
            //    c.HasKey(x => x.Id);
            //    c.Property(x => x.Id).HasColumnName("Id");
            //    c.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
            //    c.Property(x => x.UserName).HasColumnName("UserName").HasMaxLength(20);
            //    c.Property(x => x.Age).HasColumnName("Age");
            //    c.Property(x => x.Sex).HasColumnName("Sex");
            //    c.Property(x => x.PhoneNumber).HasColumnName("PhoneNumber");
            //    c.Property(x => x.Email).HasColumnName("Email").HasMaxLength(50);
            //    c.Property(x => x.IsVip).HasColumnName("IsVip");
            //    c.Property(x => x.IsBlack).HasColumnName("IsBlack");
            //    c.Property(x => x.Country).HasColumnName("Country");
            //    c.Property(x => x.AccountBalance).HasColumnName("AccountBalance");
            //    c.Property(x => x.AccountBalance2).HasColumnName("AccountBalance2");
            //    c.Property(x => x.Status).HasColumnName("Status");
            //    c.Property(x => x.Remark).HasColumnName("Remark").HasMaxLength(255);
            //    c.Property(x => x.CreateTime).HasColumnName("CreateTime");
            //    c.Property(x => x.UpdateTime).HasColumnName("UpdateTime");
            //    c.ToTable("Test");
            //});

            base.OnModelCreating(modelBuilder);
        }

        //public DbSet<TestUpperEntity> TestEntities { get; set; }
        public DbSet<TestEntity> TestEntities { get; set; }

        #region 配置数据库连接
#if UseMySQL
        private void UseMySQL(DbContextOptionsBuilder optionsBuilder)
        {
            // MySQL: CRUD test passed.
            var connString = "server=127.0.0.1;database=test;user id=root;password=12345!a";
            optionsBuilder.UseMySQL(connString);
        }
#endif

#if UseMariaDB
        private void UseMariaDB(DbContextOptionsBuilder optionsBuilder)
        {
            // MariaDB: CRUD test passed.
            // var connString = "server=127.0.0.1;port=3306;database=test;user=root;password=12345!a";// √
            var connString = "server=127.0.0.1;database=test;user id=root;password=12345!a";// √
            optionsBuilder.UseMySql(connString, ServerVersion.AutoDetect(connString));
        }
#endif

#if UseTiDB
        private void UseTiDB(DbContextOptionsBuilder optionsBuilder)
        {
            // TiDB: CRUD test passed.
            var connString = "server=127.0.0.1;port=4000;database=test;user id=root;password=";
            optionsBuilder.UseMySQL(connString);
        }
#endif

#if UseSqlServer
        private void UseSqlServer(DbContextOptionsBuilder optionsBuilder)
        {
            // SQL Server: CRUD test passed.
            var connString = "server=127.0.0.1;database=test;uid=sa;pwd=12345!a;TrustServerCertificate=true";
            //optionsBuilder.UseSqlServer(connString);// SQL Server 2012 ~ +
            optionsBuilder.UseSqlServer(connString, c => c.UseRowNumberForPaging());// SQL Server 2005 ~ 2008
        }
#endif

#if UseOracle
        private void UseOracle(DbContextOptionsBuilder optionsBuilder)
        {
            // Oracle: CRUD test passed.
            //var connString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User ID=SYSTEM;Password=12345!a;Persist Security Info=True";
            var connString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User ID=TEST;Password=12345!a;Persist Security Info=True";
            optionsBuilder.UseOracle(connString, builder => builder.UseOracleSQLCompatibility("11"));// 指定数据库版本：Oracle Database 11g
            //optionsBuilder.UseOracle(connString, builder => builder.UseOracleSQLCompatibility("12"));// 指定数据库版本：Oracle Database 12c
        }
#endif

#if UseSqlite
        private void UseSqlite(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite: CRUD test passed.
            var connString = "data source=.\\test.db";
            optionsBuilder.UseSqlite(connString);
        }
#endif

#if UseMsAccess
        private void UseMsAccess(DbContextOptionsBuilder optionsBuilder)
        {
            // MS Access: CRUD test passed.
            // 1. 使用 Microsoft.ACE.OLEDB 需要安装：AccessDatabaseEngine.exe（x86\x64）      https://www.microsoft.com/zh-CN/download/details.aspx?id=13255
            //var connString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=.\\Test.mdb";// MS Access 2003
            var connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=.\\Test.mdb";// MS Access 2003
            //var connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=.\\Test.accdb";// MS Access 2007+
            optionsBuilder.UseJet(connString, DataAccessProviderType.OleDb);
        }
#endif

#if UseFirebird
        private void UseFirebird(DbContextOptionsBuilder optionsBuilder)
        {
            // Firebird 提供多种服务器环境版本：服务器版本（Classic\SuperClassic\SuperServer）、嵌入式版本（Embedded）

            // Embedded Firebird:
            // 
            // 1. Download Server Package from https://firebirdsql.org/
            // 2. Copying files:
            // 
            // Firebird-4.0.2.2816-0-x64.zip
            // |--intl
            // |  |--fbintl.conf
            // |  \--fbintl.dll
            // |--plugins
            // |  |--udr
            // |     |--udf_compat.dll
            // |     |--UdfBackwardCompatibility.sql
            // |     \--udrcpp_example.dll
            // |  |--chacha.dll
            // |  |--engine13.dll
            // |  |--fbtrace.dll
            // |  |--legacy_auth.dll
            // |  |--legacy_usermanager.dll
            // |  |--srp.dll
            // |  |--udr_engine.conf
            // |  \--udr_engine.dll
            // |--fbclient.dll
            // |--ib_util.dll
            // |--icudt63.dll
            // |--icuin63.dll
            // |--icuuc63.dll
            // |--msvcp140.dll
            // |--vcruntime140.dll
            // \--zlib1.dll

            // Firebird(Embedded version): CRUD test passed.
            var sb = new FbConnectionStringBuilder
            {
                //Database = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.fdb"),
                Database = @".\test.fdb",
                UserID = "sysdba",
                //Password = "masterkey",
                Pooling = true,
                ServerType = FbServerType.Embedded,
                ClientLibrary = "fbclient.dll"
            };
            optionsBuilder.UseFirebird(sb.ToString());

            //// Firebird(Embedded version): CRUD test passed.
            //var connString = "initial catalog=.\\test.fdb;user id=sysdba;pooling=True;server type=Embedded;client library=fbclient.dll";
            //optionsBuilder.UseFirebird(connString);

            //// Firebird(Server version): CRUD test passed.
            //var connString = "database=localhost:demo.fdb;user=sysdba;password=masterkey";
            //optionsBuilder.UseFirebird(connString);
        }
#endif

#if UsePostgreSql
        private void UsePostgreSql(DbContextOptionsBuilder optionsBuilder)
        {
            // PostgreSql: CRUD test passed.
            var connString = "server=127.0.0.1;database=postgres;username=postgres;password=12345!a";
            optionsBuilder.UseNpgsql(connString);
        }
#endif

#if UseDB2
        private void UseDB2(DbContextOptionsBuilder optionsBuilder)
        {
            // DB2 Express-C v11.1.3030: CRUD test passed.
            //var sb = new DB2ConnectionStringBuilder
            //{
            //    Server = "127.0.0.1",
            //    Database = "sample",
            //    UserID = "db2admin",
            //    Password = "12345!a"
            //};
            var connString = "Server=127.0.0.1;Database=sample;UID=db2admin;PWD=12345!a";
            optionsBuilder.UseDb2(connString, builder =>
            {
                builder.SetServerInfo(IBMDBServerType.LUW);
                //builder.UseRowNumberForPaging();
            });
        }
#endif

#if UseInformix
        private void UseInformix(DbContextOptionsBuilder optionsBuilder)
        {
            // Informix
            // 
            // https://www.ibm.com/cn-zh/products/informix/editions
            // 
            // 用于开发和测试的免费版本：
            // 1. Informix Developer Edition
            // 2. Informix Innovator-C Edition
            // 
            // 安装数据库后设置账号\密码：
            // 1. informix\12345!a
            // 2. ifxjson\12345!a

            var connString = "Server=127.0.0.1:9088;Database=Test;UserID=informix;Password=12345!a";
            optionsBuilder.UseDb2(connString, builder =>
            {
                builder.SetServerInfo(IBMDBServerType.IDS);// IBM Informix Dynamic Server
                //builder.UseRowNumberForPaging();
            });
        }
#endif

#if UseClickHouse
        private void UseClickHouse(DbContextOptionsBuilder optionsBuilder)
        {
            // ClickHouse: CRUD test passed.
            var sb = new ClickHouseConnectionStringBuilder
            {
                Host = "localhost",
                Port = 8123,
                Database = "default",
            };
            var connString = sb.ConnectionString;
            //var connString = "Host=localhost;Port=8123;Database=default;User=default;Password=";
            optionsBuilder.UseClickHouse(connString);
        }
#endif

#if UseDm
        private void UseDm(DbContextOptionsBuilder optionsBuilder)
        {
            // DM（达梦）: CRUD test passed.
            var connString = "SERVER=127.0.0.1;PORT=5236;USER=SYSDBA;PASSWORD=SYSDBA";
            optionsBuilder.UseDm(connString);
        }
#endif

#if UseKingbaseES
        private void UseKingbaseES(DbContextOptionsBuilder optionsBuilder)
        {
            // KingbaseES（人大金仓）: CRUD test passed.
            // 
            // 数据库兼容模式：
            // Oracle[ ]       PG[√]
            var connString = "Server=127.0.0.1;port=54321;Database=test;UID=system;password=12345!a";
            optionsBuilder.UseKdbndp(connString);
        }
#endif
        #endregion
    }
}
