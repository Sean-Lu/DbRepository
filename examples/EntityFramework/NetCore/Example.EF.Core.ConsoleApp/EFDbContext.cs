using System.Diagnostics;
using EntityFrameworkCore.Jet.Data;
using EntityFrameworkCore.UseRowNumberForPaging;
using Example.EF.Core.ConsoleApp.Entities;
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

            #region 配置数据库连接
            //// MySQL: CRUD test passed.
            //var connString = "server=127.0.0.1;database=test;user id=root;password=12345!a";
            //optionsBuilder.UseMySQL(connString);

            // SQLite: CRUD test passed.
            var connString = "data source=.\\test.db";
            optionsBuilder.UseSqlite(connString);

            //// SQL Server: CRUD test passed.
            //var connString = "server=127.0.0.1;database=test;uid=sa;pwd=12345!a;TrustServerCertificate=true";
            ////optionsBuilder.UseSqlServer(connString);// SQL Server 2012 ~ +
            //optionsBuilder.UseSqlServer(connString, c => c.UseRowNumberForPaging());// SQL Server 2005 ~ 2008

            //// MS Access: CRUD test passed.
            //// 1. 使用 Microsoft.ACE.OLEDB 需要安装：AccessDatabaseEngine.exe（x86\x64）      https://www.microsoft.com/zh-CN/download/details.aspx?id=13255
            ////var connString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=.\\Test.mdb";// MS Access 2003
            //var connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=.\\Test.mdb";// MS Access 2003
            ////var connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=.\\Test.accdb";// MS Access 2007+
            //optionsBuilder.UseJet(connString, DataAccessProviderType.OleDb);
            #endregion

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
    }
}
