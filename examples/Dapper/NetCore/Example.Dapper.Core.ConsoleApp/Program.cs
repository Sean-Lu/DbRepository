using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Example.Dapper.Core.Application.Extensions;
using Example.Dapper.Core.ConsoleApp.Impls;
using Example.Dapper.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.ConsoleApp
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

            ISimpleDo toDo = DIManager.GetService<DBTest>();
            toDo.Execute();

            Console.ReadLine();
        }
    }
}
