using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
#if NETSTANDARD || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

namespace Sean.Core.DbRepository.Dapper;

public abstract class DapperBaseRepository : BaseRepository
{
    #region Constructors
#if NETSTANDARD || NET5_0_OR_GREATER
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected DapperBaseRepository(IConfiguration configuration, string configName = Constants.Master) : base(configuration, configName)
    {
    }
#else
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected DapperBaseRepository(string configName = Constants.Master) : base(configName)
    {
    }
#endif
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="connectionSettings"></param>
    protected DapperBaseRepository(MultiConnectionSettings connectionSettings) : base(connectionSettings)
    {
    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="option"></param>
    protected DapperBaseRepository(ConnectionStringOptions option) : base(option)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="type"></param>
    protected DapperBaseRepository(string connString, DatabaseType type) : base(connString, type)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="factory"></param>
    protected DapperBaseRepository(string connString, DbProviderFactory factory) : base(connString, factory)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="providerName"></param>
    protected DapperBaseRepository(string connString, string providerName) : base(connString, providerName)
    {

    }
    #endregion

    #region Synchronous method
    public override int Execute(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.Execute(this, sqlCommand);
    public override IEnumerable<T> Query<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.Query<T>(this, sqlCommand);
    public override T Get<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.Get<T>(this, sqlCommand);
    public override T ExecuteScalar<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalar<T>(this, sqlCommand);
    public override object ExecuteScalar(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalar(this, sqlCommand);
    public override DataTable ExecuteDataTable(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataTable(this, sqlCommand);
    public override DataSet ExecuteDataSet(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataSet(this, sqlCommand);
    public override IDataReader ExecuteReader(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteReader(this, sqlCommand);
    #endregion

    #region Asynchronous method
    public override Task<int> ExecuteAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteAsync(this, sqlCommand);
    public override Task<IEnumerable<T>> QueryAsync<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.QueryAsync<T>(this, sqlCommand);
    public override Task<T> GetAsync<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.GetAsync<T>(this, sqlCommand);
    public override Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalarAsync<T>(this, sqlCommand);
    public override Task<object> ExecuteScalarAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalarAsync(this, sqlCommand);
    public override Task<DataTable> ExecuteDataTableAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataTableAsync(this, sqlCommand);
    public override Task<DataSet> ExecuteDataSetAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataSetAsync(this, sqlCommand);
    public override Task<IDataReader> ExecuteReaderAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteReaderAsync(this, sqlCommand);
    #endregion
}

public abstract class DapperBaseRepository<TEntity> : BaseRepository<TEntity> where TEntity : class
{
    #region Constructors
#if NETSTANDARD || NET5_0_OR_GREATER
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected DapperBaseRepository(IConfiguration configuration, string configName = Constants.Master) : base(configuration, configName)
    {
    }
#else
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="configName">Configuration ConnectionStrings name</param>
    protected DapperBaseRepository(string configName = Constants.Master) : base(configName)
    {
    }
#endif
    /// <summary>
    /// Single or clustered database.
    /// </summary>
    /// <param name="connectionSettings"></param>
    protected DapperBaseRepository(MultiConnectionSettings connectionSettings) : base(connectionSettings)
    {
    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="option"></param>
    protected DapperBaseRepository(ConnectionStringOptions option) : base(option)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="type"></param>
    protected DapperBaseRepository(string connString, DatabaseType type) : base(connString, type)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="factory"></param>
    protected DapperBaseRepository(string connString, DbProviderFactory factory) : base(connString, factory)
    {

    }
    /// <summary>
    /// Single database.
    /// </summary>
    /// <param name="connString"></param>
    /// <param name="providerName"></param>
    protected DapperBaseRepository(string connString, string providerName) : base(connString, providerName)
    {

    }
    #endregion

    #region Synchronous method
    public override int Execute(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.Execute(this, sqlCommand);
    public override IEnumerable<T> Query<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.Query<T>(this, sqlCommand);
    public override T Get<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.Get<T>(this, sqlCommand);
    public override T ExecuteScalar<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalar<T>(this, sqlCommand);
    public override object ExecuteScalar(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalar(this, sqlCommand);
    public override DataTable ExecuteDataTable(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataTable(this, sqlCommand);
    public override DataSet ExecuteDataSet(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataSet(this, sqlCommand);
    public override IDataReader ExecuteReader(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteReader(this, sqlCommand);
    #endregion

    #region Asynchronous method
    public override Task<int> ExecuteAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteAsync(this, sqlCommand);
    public override Task<IEnumerable<T>> QueryAsync<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.QueryAsync<T>(this, sqlCommand);
    public override Task<T> GetAsync<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.GetAsync<T>(this, sqlCommand);
    public override Task<T> ExecuteScalarAsync<T>(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalarAsync<T>(this, sqlCommand);
    public override Task<object> ExecuteScalarAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteScalarAsync(this, sqlCommand);
    public override Task<DataTable> ExecuteDataTableAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataTableAsync(this, sqlCommand);
    public override Task<DataSet> ExecuteDataSetAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteDataSetAsync(this, sqlCommand);
    public override Task<IDataReader> ExecuteReaderAsync(ISqlCommand sqlCommand)
        => DapperRepositoryExecutor.ExecuteReaderAsync(this, sqlCommand);
    #endregion
}
