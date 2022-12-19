using System.Diagnostics;
using Example.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Example.EntityFramework
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

            //var connString = "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a";
            var connString = "server=127.0.0.1;database=test;user id=root;password=12345!a";
            optionsBuilder.UseMySQL(connString/*, builder => builder.CommandTimeout(30)*/);

            //// 设置不跟踪所有查询  
            //optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            //// 启用敏感数据日志记录
            //optionsBuilder.EnableSensitiveDataLogging();

            // 记录日志，包含生成的sql          
            optionsBuilder.LogTo(msg =>
            {
                Debug.WriteLine(msg);// 调试-窗口消息
                Console.WriteLine(msg);// 输出-窗口消息
            }, LogLevel.Information);
        }

        public DbSet<TestEntity> TestEntities { get; set; }
    }
}
