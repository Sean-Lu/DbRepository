using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Sean.Core.DbRepository
{
    internal class ConfigBuilder
    {
        public static string ConfigFilePath { get; }
        public const string DbProviderMapSectionName = "dbProviderMap";

        static ConfigBuilder()
        {
            ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"dllconfigs\{Assembly.GetExecutingAssembly().GetName().Name}.dll.config");
        }

        public static T Get<T>(string sectionName)
        {
            if (ConfigurationManager.GetSection(sectionName) is T section)
            {
                return section;
            }

            return default;
        }

        public static DbProviderMapSection GetDbProviderMapSection()
        {
            var sectionName = "dbProviderMap";
            if (File.Exists(ConfigFilePath))
            {
                return ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap
                {
                    ExeConfigFilename = ConfigFilePath
                }, ConfigurationUserLevel.None).GetSection(sectionName) as DbProviderMapSection;
            }

            return Get<DbProviderMapSection>(sectionName);
        }
    }
}
