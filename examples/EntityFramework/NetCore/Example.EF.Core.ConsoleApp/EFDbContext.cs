using System.Diagnostics;
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
