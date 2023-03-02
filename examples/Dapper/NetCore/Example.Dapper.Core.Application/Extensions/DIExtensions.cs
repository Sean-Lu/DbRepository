﻿using System.Reflection;
using Example.Dapper.Core.Domain.Extensions;
using Example.Dapper.Core.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Dapper.Core.Application.Extensions
{
    public static class DIExtensions
    {
        /// <summary>
        /// 应用层依赖注入
        /// </summary>
        /// <param name="services"></param>
        public static void AddApplicationDI(this IServiceCollection services)
        {
            services.AddDomainDI();

            services.RegisterByAssemblyInterface(Assembly.GetExecutingAssembly(), "Service", ServiceLifetime.Transient);

            services.AddAutoMapper(expression =>
            {
                expression.AllowNullCollections = true;
                expression.AllowNullDestinationValues = true;
                //expression.Advanced.AllowAdditiveTypeMapCreation = true;
                expression.AddMaps(typeof(AutoMapperProfile));
            });
        }
    }
}