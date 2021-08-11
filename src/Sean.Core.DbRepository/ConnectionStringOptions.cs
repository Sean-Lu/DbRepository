namespace Sean.Core.DbRepository
{
    public class ConnectionStringOptions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        /// <param name="master">true: 主库, false: 从库</param>
        public ConnectionStringOptions(string connectionString, string providerName, bool master = true)
        {
            ConnectionString = connectionString;
            ProviderName = providerName;
            Master = master;
        }
        public ConnectionStringOptions(string connectionString, DatabaseType dbType, bool master = true)
        {
            ConnectionString = connectionString;
            DbType = dbType;
            Master = master;
        }

        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }
        public DatabaseType DbType { get; set; }
        /// <summary>
        /// true: 主库, false: 从库
        /// </summary>
        public bool Master { get; set; }
    }
}