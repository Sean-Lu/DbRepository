using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Example.Application.Extensions;
using Example.Infrastructure;
using Example.NetFramework.Impls.DbTest;
using Sean.Core.DependencyInjection;
using Sean.Utility.Contracts;

namespace Example.NetFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);// 设置当前工作目录：@".\"

            DIManager.Register(container =>
            {
                container.AddApplicationDI();

                var types = Assembly.GetExecutingAssembly().GetTypes().Where(c => c.IsClass && typeof(ISimpleDo).IsAssignableFrom(c)).ToList();
                types.ForEach(c =>
                {
                    container.RegisterType(c, c, ServiceLifeStyle.Transient);
                });
            });

            //ISimpleDo toDo = DIManager.Container.Resolve<MySqlTest>();
            ISimpleDo toDo = DIManager.Container.Resolve<SQLiteTest>();
            toDo.Execute();

            Console.ReadLine();
        }

        private static void TestDateTime()
        {
            #region 利用反射机制修改 DateTime.ToString() 的默认格式
            Console.WriteLine($"修改默认时间格式之前：{DateTime.Now}");
            if (DateTimeFormatInfo.CurrentInfo != null)
            {
                var type = DateTimeFormatInfo.CurrentInfo.GetType();
                var field = type.GetField("generalLongTimePattern", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(DateTimeFormatInfo.CurrentInfo, "yyyy-MM-dd HH:mm:ss");
                }
            }
            Console.WriteLine($"修改默认时间格式之后：{DateTime.Now}");
            #endregion
        }
    }
}
