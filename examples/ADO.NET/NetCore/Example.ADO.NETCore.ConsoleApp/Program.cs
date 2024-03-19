using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Example.ADO.NETCore.Application.Extensions;
using Example.ADO.NETCore.ConsoleApp.Impls;
using Example.ADO.NETCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);// 设置当前工作目录：@".\"

            DIManager.ConfigureServices((services, configuration) =>
            {
                services.AddApplicationDI();

                var types = Assembly.GetExecutingAssembly().GetTypes().Where(c => c.IsClass && typeof(ISimpleDo).IsAssignableFrom(c)).ToList();
                types.ForEach(c =>
                {
                    services.AddTransient(c);
                });
            });

            //ISimpleDo test = DIManager.GetService<DbFirstTest>();
            //ISimpleDo test = DIManager.GetService<CodeFirstTest>();
            //ISimpleDo test = DIManager.GetService<Domain.DBTest.OracleTest>();
            ISimpleDo test = DIManager.GetService<DBTest>();
            test.Execute();

            Console.ReadLine();
        }
    }
}
