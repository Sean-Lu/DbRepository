using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Example.Application.Extensions;
using Example.NetCore.Impls.DbTest;
using Microsoft.Extensions.DependencyInjection;
using Sean.Core.Ioc;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Impls.Log;

namespace Example.NetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);// 设置当前工作目录：@".\"

            IocContainer.Instance.ConfigureServices(services =>
            {
                services.AddApplicationDI();

                var types = Assembly.GetExecutingAssembly().GetTypes().Where(c => c.IsClass && typeof(ISimpleDo).IsAssignableFrom(c)).ToList();
                types.ForEach(c =>
                {
                    services.AddTransient(c);
                });
            });

            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            #endregion

            ISimpleDo toDo = IocContainer.Instance.GetService<MySqlTest>();
            //ISimpleDo toDo = IocContainer.Instance.GetService<SQLiteTest>();
            toDo.Execute();

            Console.ReadLine();
        }
    }
}
