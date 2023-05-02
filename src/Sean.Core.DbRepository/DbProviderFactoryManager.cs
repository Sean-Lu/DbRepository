using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Config;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// <see cref="DbProviderFactory"/>
    /// </summary>
    internal static class DbProviderFactoryManager
    {
        internal static readonly ConcurrentDictionary<DatabaseType, DbProviderMap> DbProviderMapDic;

        static DbProviderFactoryManager()
        {
            DbProviderMapDic = new ConcurrentDictionary<DatabaseType, DbProviderMap>();

            LoadFromConfigurationFile();
        }

        public static DbProviderFactory GetDbProviderFactory(DatabaseType type)
        {
            DbProviderFactory factory = DbContextConfiguration.Options.MapToDbProviderFactory?.Invoke(type);
            if (factory != null)
            {
                return factory;
            }

            if (DbProviderMapDic.TryGetValue(type, out var map) && map != null)
            {
                if (map.ProviderFactory == null && !string.IsNullOrWhiteSpace(map.ProviderInvariantName))
                {
                    map.ProviderFactory = GetDbProviderFactory(map.ProviderInvariantName);
                }

                factory = map.ProviderFactory;
            }

            return factory ?? throw new Exception($"The mapping from DatabaseType.{type} to DbProviderFactory is not found.");
        }
        public static DbProviderFactory GetDbProviderFactory(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                return null;
            }

            var map = DbProviderMapDic.FirstOrDefault(c => c.Value?.ProviderInvariantName == providerName).Value;
            if (map != null)
            {
                if (map.ProviderFactory != null)
                {
                    return map.ProviderFactory;
                }

                if (!string.IsNullOrWhiteSpace(map.FactoryTypeAssemblyQualifiedName))
                {
                    var type = Type.GetType(map.FactoryTypeAssemblyQualifiedName);
                    if (type != null)
                    {
                        #region 1. Try to get an instance of the DbProviderFactory by reflection.
                        var dbProvideFactoryInstanceFieldInfo = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                        if (dbProvideFactoryInstanceFieldInfo != null)
                        {
                            var dbProvideFactoryInstanceValue = dbProvideFactoryInstanceFieldInfo.GetValue(type);
                            if (dbProvideFactoryInstanceValue is DbProviderFactory dbProviderFactory)
                            {
                                map.ProviderFactory = dbProviderFactory;
                                return map.ProviderFactory;
                            }
                        }
                        #endregion

                        #region 2. Try to get an instance of the DbProviderFactory by the default parameterless constructor.
                        if (Activator.CreateInstance(type) is DbProviderFactory instance)
                        {
                            map.ProviderFactory = instance;
                            return map.ProviderFactory;
                        }
                        #endregion
                    }
                }
            }

#if NETSTANDARD2_0
            // 注：从.NET Standard 2.1版本开始（.NET Core >= 3.0）才有DbProviderFactories
            throw new Exception($"The mapping from '{providerName}' to DbProviderFactory is not found.");
#else
            return DbProviderFactories.GetFactory(providerName);
#endif
        }

        private static void LoadFromConfigurationFile()
        {
#if NETSTANDARD || NET5_0_OR_GREATER
            if (File.Exists(DbContextConfiguration.Options.DbProviderFactoryConfigurationPath))
            {
                //var xpathTemplate = "/configuration/dbProviderMap/databases/database[@name='{0}']";
                var xpathTemplate = "/configuration/dbProviderMap/databases/database";
                var xmlNodeList = XmlHelper.GetXmlNodeList(DbContextConfiguration.Options.DbProviderFactoryConfigurationPath, xpathTemplate);
                if (xmlNodeList != null)
                {
                    foreach (XmlNode xmlNode in xmlNodeList)
                    {
                        var name = XmlHelper.GetXmlAttributeValue(xmlNode, "name");
                        if (Enum.TryParse(name, out DatabaseType dbType))
                        {
                            var providerInvariantName = XmlHelper.GetXmlAttributeValue(xmlNode, "providerInvariantName");
                            var factoryTypeAssemblyQualifiedName = XmlHelper.GetXmlAttributeValue(xmlNode, "factoryTypeAssemblyQualifiedName");
                            var map = new DbProviderMap(providerInvariantName, factoryTypeAssemblyQualifiedName);
                            dbType.SetDbProviderMap(map);
                        }
                    }
                }
            }
#else
            var section = ConfigBuilder.GetDbProviderMapSection();
            if (section == null)
            {
                #region Set default configuration.
                //section = new DbProviderMapSection();
                //section.Databases.Clear();
                //section.Databases.Add(new DatabaseElement(DatabaseType.MySql, "MySql.Data.MySqlClient", "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.SqlServer, "System.Data.SqlClient", "System.Data.SqlClient.SqlClientFactory,System.Data"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.Oracle, "Oracle.ManagedDataAccess.Client", "Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.SQLite, "System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.MsAccess, "System.Data.OleDb", "System.Data.OleDb.OleDbFactory,System.Data"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.Firebird, "FirebirdSql.Data.FirebirdClient", "FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,FirebirdSql.Data.FirebirdClient"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.PostgreSql, "Npgsql", "Npgsql.NpgsqlFactory,Npgsql"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.DB2, "IBM.Data.DB2", "IBM.Data.DB2.Core.DB2Factory,IBM.Data.DB2.Core"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.Informix, "IBM.Data.Informix", "IBM.Data.Informix.IfxFactory,IBM.Data.Informix"));
                #endregion
            }

            var databases = section?.Databases?.OfType<DatabaseElement>().ToList();
            databases?.ForEach(element =>
            {
                if (element.DbType.HasValue)
                {
                    var dbType = element.DbType.Value;
                    var map = new DbProviderMap(element.ProviderInvariantName, element.FactoryTypeAssemblyQualifiedName);
                    dbType.SetDbProviderMap(map);
                }
            });
#endif
        }
    }
}
