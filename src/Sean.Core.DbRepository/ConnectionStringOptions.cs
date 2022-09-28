using System.Data.Common;

namespace Sean.Core.DbRepository
{
    /// <summary>
    /// 数据库连接配置
    /// </summary>
    public class ConnectionStringOptions
    {
        /// <summary>
        /// 数据库连接配置
        /// </summary>
        /// <param name="connectionString">数据库连接字符串，需要在该参数中添加 <see cref="DatabaseType"/> 或 ProviderName 属性来指定数据库连接驱动。</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        public ConnectionStringOptions(string connectionString, bool master = true) : this(connectionString, (string)null, master)
        {

        }
        /// <summary>
        /// 数据库连接配置
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="providerName">数据库提供者名称</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        public ConnectionStringOptions(string connectionString, string providerName, bool master = true)
        {
            ConnectionString = connectionString;
            ProviderName = providerName;
            Master = master;
        }
        /// <summary>
        /// 数据库连接配置
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        public ConnectionStringOptions(string connectionString, DatabaseType dbType, bool master = true)
        {
            ConnectionString = connectionString;
            DbType = dbType;
            Master = master;
        }
        /// <summary>
        /// 数据库连接配置
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="providerFactory">数据库提供者工厂</param>
        /// <param name="master">true: use master database, false: use slave database.</param>
        public ConnectionStringOptions(string connectionString, DbProviderFactory providerFactory, bool master = true)
        {
            ConnectionString = connectionString;
            ProviderFactory = providerFactory;
            Master = master;
        }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get; internal set; }
        /// <summary>
        /// 数据库提供者名称
        /// </summary>
        public string ProviderName { get; internal set; }
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DbType { get; internal set; }
        /// <summary>
        /// 数据库提供者工厂
        /// </summary>
        public DbProviderFactory ProviderFactory { get; }
        /// <summary>
        /// true: use master database, false: use slave database.
        /// </summary>
        public bool Master { get; }
    }
}