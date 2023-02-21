using Example.Infrastructure.Impls;
using Sean.Core.DependencyInjection;
using Sean.Utility.Contracts;
using Sean.Utility.Extensions;
using Sean.Utility.Impls.Log;

namespace Example.Infrastructure.Extensions
{
    public static class DIExtensions
    {
        /// <summary>
        /// 基础设施层依赖注入
        /// </summary>
        /// <param name="container"></param>
        public static void AddInfrastructureDI(this IDIRegister container)
        {
            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            SimpleLocalLoggerBase.DefaultLoggerOptions = new SimpleLocalLoggerOptions
            {
                LogToConsole = true,
                LogToLocalFile = true
            };
            #endregion

            // Logger注入
            container.RegisterType(typeof(ILogger<>), typeof(SimpleLocalLogger<>), ServiceLifeStyle.Transient);

            container.RegisterType<IJsonSerializer, NewJsonSerializer>(ServiceLifeStyle.Transient);
        }
    }
}
