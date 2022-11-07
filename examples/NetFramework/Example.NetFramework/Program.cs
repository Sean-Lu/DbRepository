using System;
using Example.NetFramework.Impls;
using Example.NetFramework.Impls.DbTest;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Impls.Log;

namespace Example.NetFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            SimpleLocalLoggerBase.DefaultLoggerOptions = new SimpleLocalLoggerOptions
            {
                LogToConsole = true,
                LogToLocalFile = true
            };
            #endregion

            #region 配置数据库驱动映射关系
            // 方式1：在配置文件中配置映射关系
            // 方式2：在代码里直接指定映射关系
            //DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));
            //DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));
            ////DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));
            #endregion

            DbFactory.JsonSerializer = NewJsonSerializer.Instance;

            ISimpleDo toDo = new MySqlTest();
            //ISimpleDo toDo = new SQLiteTest();
            toDo.Execute();

            Console.ReadLine();
        }
    }
}
