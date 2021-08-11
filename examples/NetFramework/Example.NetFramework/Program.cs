using System;
using Example.NetFramework.Impls.DbTest;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Impls.Log;

namespace Example.NetFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            SimpleLocalLoggerBase.DefaultLoggerOptions = new SimpleLocalLoggerOptions
            {
                LogToConsole = true,
                LogToLocalFile = false
            };

            ISimpleDo toDo = new MySqlTest();
            toDo.Execute();

            Console.ReadLine();
        }
    }
}
