﻿using System.Diagnostics;
using EntityFrameworkCore.Jet.Data;
using EntityFrameworkCore.UseRowNumberForPaging;
using Example.EF.Core.ConsoleApp.Entities;
using IBM.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Example.EF.Core.ConsoleApp
{
    public class EFDbContext : DbContext
    {
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

            // 记录日志，包含生成的sql
            optionsBuilder.LogTo(msg =>
            {
                Debug.WriteLine(msg);// 输出调试窗口消息
                Console.WriteLine(msg);// 输出控制台窗口消息
            }, LogLevel.Information);
        }

        public DbSet<TestEntity> TestEntities { get; set; }

        #region 配置数据库连接
        private void UseMySQL(DbContextOptionsBuilder optionsBuilder)
        {
            // MySQL: CRUD test passed.
            var connString = "server=127.0.0.1;database=test;user id=root;password=12345!a";
            optionsBuilder.UseMySQL(connString);
        }

        private void UseSqlServer(DbContextOptionsBuilder optionsBuilder)
        {
            // SQL Server: CRUD test passed.
            var connString = "server=127.0.0.1;database=test;uid=sa;pwd=12345!a;TrustServerCertificate=true";
            //optionsBuilder.UseSqlServer(connString);// SQL Server 2012 ~ +
            optionsBuilder.UseSqlServer(connString, c => c.UseRowNumberForPaging());// SQL Server 2005 ~ 2008
        }

        private void UseOracle(DbContextOptionsBuilder optionsBuilder)
        {
            // Oracle: CRUD test passed.
            //var connString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User ID=SYSTEM;Password=12345!a;Persist Security Info=True";
            var connString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User ID=TEST;Password=12345!a;Persist Security Info=True";
            optionsBuilder.UseOracle(connString, builder => builder.UseOracleSQLCompatibility("11"));// 指定数据库版本：Oracle Database 11g
            //optionsBuilder.UseOracle(connString, builder => builder.UseOracleSQLCompatibility("12"));// 指定数据库版本：Oracle Database 12c
        }

        private void UseSqlite(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite: CRUD test passed.
            var connString = "data source=.\\test.db";
            optionsBuilder.UseSqlite(connString);
        }

        private void UseMsAccess(DbContextOptionsBuilder optionsBuilder)
        {
            // MS Access: CRUD test passed.
            // 1. 使用 Microsoft.ACE.OLEDB 需要安装：AccessDatabaseEngine.exe（x86\x64）      https://www.microsoft.com/zh-CN/download/details.aspx?id=13255
            //var connString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=.\\Test.mdb";// MS Access 2003
            var connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=.\\Test.mdb";// MS Access 2003
            //var connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=.\\Test.accdb";// MS Access 2007+
            optionsBuilder.UseJet(connString, DataAccessProviderType.OleDb);
        }

        private void UsePostgreSql(DbContextOptionsBuilder optionsBuilder)
        {
            // PostgreSql: CRUD test passed.
            var connString = "server=127.0.0.1;database=postgres;username=postgres;password=12345!a";
            optionsBuilder.UseNpgsql(connString);
        }

        private void UseDB2(DbContextOptionsBuilder optionsBuilder)
        {
            // DB2 Express-C v11.1.3030
            var connString = "Server=127.0.0.1;Database=sample;UID=db2admin;PWD=12345!a";
            optionsBuilder.UseDb2(connString, builder =>
            {
                //builder.SetServerInfo(IBMDBServerType.LUW);
                //builder.UseRowNumberForPaging()
            });
        }
        #endregion
    }
}
