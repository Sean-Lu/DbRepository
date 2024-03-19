using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example.ADO.NETCore.Infrastructure
{
    public static class DIManager
    {
        private static IConfiguration _configuration;
        private static IServiceCollection _services;
        private static IServiceProvider _serviceProvider;

        public static void ConfigureServices(Action<IServiceCollection, IConfiguration> configServices)
        {
            if (_services != null)
            {
                configServices(_services, _configuration);
                _serviceProvider = _services.BuildServiceProvider();
                return;
            }

            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            _services = new ServiceCollection();
            _services.AddSingleton<IConfiguration>(_configuration);
            configServices(_services, _configuration);

            _serviceProvider = _services.BuildServiceProvider();
        }

        public static TService GetService<TService>()
        {
            return _serviceProvider.GetService<TService>();
        }
    }
}
