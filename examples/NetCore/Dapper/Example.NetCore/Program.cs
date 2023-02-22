using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Example.Application.Extensions;
using Example.Infrastructure;
using Example.NetCore.Impls.DbTest;
using Microsoft.Extensions.DependencyInjection;
using Sean.Utility.Contracts;

namespace Example.NetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);// 设置当前工作目录：@".\"

            DIManager.ConfigureServices(services =>
            {
                services.AddApplicationDI();

                var types = Assembly.GetExecutingAssembly().GetTypes().Where(c => c.IsClass && typeof(ISimpleDo).IsAssignableFrom(c)).ToList();
                types.ForEach(c =>
                {
                    services.AddTransient(c);
                });
            });

            ISimpleDo toDo = DIManager.GetService<MySqlTest>();
            //ISimpleDo toDo = DIManager.GetService<SQLiteTest>();
            toDo.Execute();

            Console.ReadLine();
        }
    }
}
