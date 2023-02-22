using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Example.Infrastructure.Impls;
using Microsoft.Extensions.DependencyInjection;
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
        /// <param name="services"></param>
        public static void AddInfrastructureDI(this IServiceCollection services)
        {
            #region 配置Logger
            SimpleLocalLoggerBase.DateTimeFormat = time => time.ToLongDateTime();
            #endregion

            services.AddSimpleLocalLogger();

            services.AddTransient<IJsonSerializer, NewJsonSerializer>();
        }

        public static IServiceCollection RegisterByAssemblyInterface(this IServiceCollection services, Assembly assembly, string interfaceSuffix, ServiceLifetime serviceLifetime)
        {
            var types = assembly.GetTypes();
            var interfaceTypes = types.Where(c => c.IsInterface && c.Name.EndsWith(interfaceSuffix));
            foreach (var interfaceType in interfaceTypes)
            {
                var implType = types.FirstOrDefault(c => c.Name == interfaceType.Name.Substring(1));
                if (implType != null)
                {
                    switch (serviceLifetime)
                    {
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(interfaceType, implType);
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(interfaceType, implType);
                            break;
                        case ServiceLifetime.Scoped:
                            services.AddScoped(interfaceType, implType);
                            break;
                    }
                }
            }
            return services;
        }
        public static IServiceCollection RegisterByAssemblyClass(this IServiceCollection services, Assembly assembly, string classSuffix, ServiceLifetime serviceLifetime)
        {
            var serviceImplList = assembly.GetTypes().Where(s => s.IsClass && !s.IsInterface && !s.IsAbstract && s.Name.EndsWith(classSuffix)).ToList();

            var result = new Dictionary<Type, Type[]>();
            foreach (var serviceImpl in serviceImplList)
            {
                var interfaceTypeArray = serviceImpl.GetInterfaces().Where(t => t.Name.EndsWith(classSuffix)).ToArray();
                result.Add(serviceImpl, interfaceTypeArray);
            }

            foreach (var implementationType in result)
            {
                foreach (var serviceType in implementationType.Value)
                {
                    switch (serviceLifetime)
                    {
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(serviceType, implementationType.Key);
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(serviceType, implementationType.Key);
                            break;
                        case ServiceLifetime.Scoped:
                            services.AddScoped(serviceType, implementationType.Key);
                            break;
                    }
                }
            }

            return services;
        }
    }
}
