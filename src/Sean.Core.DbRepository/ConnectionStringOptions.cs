using System.Data.Common;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// Database connection string option.
    /// </summary>
    public class ConnectionStringOptions
    {
        /// <summary>
        /// Database connection string option.
        /// </summary>
        /// <param name="connectionString">Database connection string. The database connection string must contain the value of DatabaseType or ProviderName to correctly match the specified database connection driver.</param>
        /// <param name="master">true: master database, false: slave database.</param>
        public ConnectionStringOptions(string connectionString, bool master = true) : this(connectionString, (string)null, master)
        {

        }
        /// <summary>
        /// Database connection string option.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="providerName">Database provider name.</param>
        /// <param name="master">true: master database, false: slave database.</param>
        public ConnectionStringOptions(string connectionString, string providerName, bool master = true)
        {
            ConnectionString = connectionString;
            ProviderName = providerName;
            Master = master;
        }
        /// <summary>
        /// Database connection string option.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="dbType">Database type.</param>
        /// <param name="master">true: master database, false: slave database.</param>
        public ConnectionStringOptions(string connectionString, DatabaseType dbType, bool master = true)
        {
            ConnectionString = connectionString;
            DbType = dbType;
            Master = master;
        }
        /// <summary>
        /// Database connection string option.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="providerFactory">Database provider factory.</param>
        /// <param name="master">true: master database, false: slave database.</param>
        public ConnectionStringOptions(string connectionString, DbProviderFactory providerFactory, bool master = true)
        {
            ConnectionString = connectionString;
            ProviderFactory = providerFactory;
            Master = master;
        }

        /// <summary>
        /// Database connection string.
        /// </summary>
        public string ConnectionString { get; internal set; }
        /// <summary>
        /// Database provider name.
        /// </summary>
        public string ProviderName { get; internal set; }
        /// <summary>
        /// Database type.
        /// </summary>
        public DatabaseType DbType { get; internal set; }
        /// <summary>
        /// Database provider factory.
        /// </summary>
        public DbProviderFactory ProviderFactory { get; }
        /// <summary>
        /// true: master database, false: slave database.
        /// </summary>
        public bool Master { get; }
    }
}