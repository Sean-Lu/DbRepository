using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Linq;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Config;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// <see cref="DbProviderFactory"/>
    /// </summary>
    public class DbProviderFactoryManager
    {
        internal static readonly ConcurrentDictionary<DatabaseType, DbProviderMap> DbProviderMapDic;

        static DbProviderFactoryManager()
        {
            DbProviderMapDic = new ConcurrentDictionary<DatabaseType, DbProviderMap>();

            LoadFromConfigurationFile();
        }

        /// <summary>
        /// Get the <see cref="DbProviderFactory"/> by specified database type
        /// </summary>
        /// <param name="type">Database type</param>
        /// <returns></returns>
        public static DbProviderFactory GetDbProviderFactory(DatabaseType type)
        {
            DbProviderFactory factory = null;
            if (DbProviderMapDic.TryGetValue(type, out var map) && map != null)
            {
                if (map.ProviderFactory == null && !string.IsNullOrWhiteSpace(map.ProviderInvariantName))
                {
                    map.ProviderFactory = GetDbProviderFactory(map.ProviderInvariantName);
                }

                factory = map.ProviderFactory;
            }
            return factory ?? throw new Exception($"[{type}] The database client driver mapping is missing, configure it in the configuration file or code before using it. {Environment.NewLine}1. Code example: DatabaseType.{type}.SetDbProviderMap(new DbProviderMap(\"{type}\", xxxFactory.Instance)); {Environment.NewLine}2. Configuration file example: {nameof(DbFactory)}.{nameof(DbFactory.ProviderFactoryConfigurationPath)} {Environment.NewLine}   https://github.com/Sean-Lu/DbRepository/blob/master/src/Sean.Core.DbRepository/dllconfigs/Sean.Core.DbRepository.dll.config");
        }
        /// <summary>
        /// Get the <see cref="DbProviderFactory"/> by specified database provider name
        /// </summary>
        /// <param name="providerName">Database provider name</param>
        /// <returns></returns>
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
                        DbProviderFactory instance = Activator.CreateInstance(type) as DbProviderFactory;
                        if (instance != null)
                        {
                            map.ProviderFactory = instance;
                            return map.ProviderFactory;
                        }
                    }
                }
            }

#if NETSTANDARD2_0
            // 注：从.NET Standard 2.1版本开始（.NET Core >= 3.0）才有DbProviderFactories
            throw new Exception("The System.Data.Common.DbProviderFactories class is not supported, because the version of .NET Standard is less than 2.1 (corresponding to the version of .NET Core less than 3.0), please directly specify the implementation class that inherits the System.Data.Common.DbProviderFactory class, example: MySql.Data.MySqlClient.MySqlClientFactory.Instance");
#else
            return DbProviderFactories.GetFactory(providerName);
#endif
        }

        /// <summary>
        /// 以XML文件的方式读取配置
        /// </summary>
        private static void LoadFromXmlFile()
        {
            if (File.Exists(DbFactory.ProviderFactoryConfigurationPath))
            {
                const string xpathTemplate = "/configuration/dbProviderMap/databases/database[@name='{0}']";
                ((DatabaseType[])Enum.GetValues(typeof(DatabaseType))).ToList().ForEach(dbType =>
                {
                    DbProviderMap map = null;
                    var xpath = string.Format(xpathTemplate, dbType.ToString());
                    var xmlNode = XmlHelper.GetXmlNode(DbFactory.ProviderFactoryConfigurationPath, xpath);
                    if (xmlNode != null)
                    {
                        var providerInvariantName = XmlHelper.GetXmlAttributeValue(xmlNode, "providerInvariantName");
                        var factoryTypeAssemblyQualifiedName = XmlHelper.GetXmlAttributeValue(xmlNode, "factoryTypeAssemblyQualifiedName");
                        map = new DbProviderMap(providerInvariantName, factoryTypeAssemblyQualifiedName);
                    }
                    dbType.SetDbProviderMap(map);
                });
            }
        }

        /// <summary>
        /// 以配置文件的方式读取配置
        /// </summary>
        private static void LoadFromConfigurationFile()
        {
            var section = ConfigBuilder.GetDbProviderMapSection();
            if (section == null)
            {
                #region 设置默认的数据库驱动配置
                //section = new DbProviderMapSection();
                //section.Databases.Clear();
                //section.Databases.Add(new DatabaseElement(DatabaseType.MySql, "MySql.Data.MySqlClient", "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.SqlServer, "System.Data.SqlClient", "System.Data.SqlClient.SqlClientFactory,System.Data"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.SqlServerCe, "Microsoft.SqlServerCe.Client", "Microsoft.SqlServerCe.Client.SqlCeClientFactory,Microsoft.SqlServerCe.Client"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.Oracle, "Oracle.ManagedDataAccess.Client", "Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.SQLite, "System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.Access, "System.Data.OleDb", "System.Data.OleDb.OleDbFactory,System.Data"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.Firebird, "FirebirdSql.Data.FirebirdClient", "FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,FirebirdSql.Data.FirebirdClient"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.PostgreSql, "Npgsql", "Npgsql.NpgsqlFactory,Npgsql"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.DB2, "IBM.Data.DB2", "IBM.Data.DB2.Core.DB2Factory,IBM.Data.DB2.Core"));
                //section.Databases.Add(new DatabaseElement(DatabaseType.Informix, "IBM.Data.Informix", "IBM.Data.Informix.IfxFactory,IBM.Data.Informix"));
                #endregion
            }

            var databases = section?.Databases?.OfType<DatabaseElement>().ToList();
            ((DatabaseType[])Enum.GetValues(typeof(DatabaseType))).ToList().ForEach(dbType =>
            {
                var element = databases?.FirstOrDefault(c => dbType == c.Name);
                if (element != null)
                {
                    var map = new DbProviderMap(element.ProviderInvariantName, element.FactoryTypeAssemblyQualifiedName);
                    dbType.SetDbProviderMap(map);
                }
            });
        }
    }
}
