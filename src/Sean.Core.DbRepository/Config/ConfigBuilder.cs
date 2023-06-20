#if NETFRAMEWORK
using System.Configuration;
using System.IO;

namespace Sean.Core.DbRepository;

internal static class ConfigBuilder
{
    private const string DbProviderMapSectionName = "dbProviderMap";

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
        var sectionName = DbProviderMapSectionName;
        if (File.Exists(DbContextConfiguration.Options.DbProviderFactoryConfigurationPath))
        {
            return ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap
            {
                ExeConfigFilename = DbContextConfiguration.Options.DbProviderFactoryConfigurationPath
            }, ConfigurationUserLevel.None).GetSection(sectionName) as DbProviderMapSection;
        }

        return Get<DbProviderMapSection>(sectionName);
    }
}
#endif
