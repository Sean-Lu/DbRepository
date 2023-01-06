using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using Example.NetFramework.Impls;
using Example.NetFramework.Impls.DbTest;
using MySql.Data.MySqlClient;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Impls.Log;

namespace Example.NetFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);// 设置当前工作目录：@".\"

            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            SimpleLocalLoggerBase.DefaultLoggerOptions = new SimpleLocalLoggerOptions
            {
                LogToConsole = true,
                LogToLocalFile = true
            };
            #endregion

            #region 配置数据库和数据库提供者工厂之间的映射关系
            DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));// MySql
            DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", SqlClientFactory.Instance));// Microsoft SQL Server
            //DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", OracleClientFactory.Instance));// Oracle
            DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));// SQLite
            //DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));// SQLite
            #endregion

            DbFactory.JsonSerializer = NewJsonSerializer.Instance;

            ISimpleDo toDo = new MySqlTest();
            //ISimpleDo toDo = new SQLiteTest();
            toDo.Execute();

            Console.ReadLine();
        }
    }
}
