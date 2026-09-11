using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Dapper.Extensions;

namespace Sean.Core.DbRepository.Dapper;

/// <summary>
/// 泛型与非泛型仓储共用的 Dapper 执行适配；不持有连接，也不另建资源管理流程。
/// </summary>
internal static class DapperRepositoryExecutor
{
    // 必须通过传入仓储的通用执行入口，保留子类重写以及连接、事务的所有权规则。
    internal static int Execute(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        var result = repository.Execute(connection => connection.Execute(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        if (repository.DbType == DatabaseType.ClickHouse && result < 1)
        {
            return 1; // 保留 ClickHouse 原有的成功结果约定。
        }
        return result;
    }

    internal static IEnumerable<T> Query<T>(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return repository.Execute(connection => connection.Query<T>(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static T Get<T>(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return repository.Execute(connection => connection.Get<T>(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static T ExecuteScalar<T>(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return repository.Execute(connection => connection.ExecuteScalar<T>(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static object ExecuteScalar(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return repository.Execute(connection => connection.ExecuteScalar(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static DataTable ExecuteDataTable(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return repository.Execute(connection => connection.ExecuteDataTable(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static DataSet ExecuteDataSet(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return repository.Execute(connection => connection.ExecuteDataSet(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static IDataReader ExecuteReader(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        // 只让 Reader 关闭仓储内部连接；连接已由通用执行方法打开，不能依赖 Dapper 自动推断。
        var behavior = sqlCommand.Transaction?.Connection == null && sqlCommand.Connection == null
            ? CommandBehavior.CloseConnection : CommandBehavior.Default;
        return repository.Execute(connection => connection.ExecuteReader(sqlCommand, repository.Factory.SqlMonitor, behavior),
            sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection, autoDisposeInternalConnection: false);
    }

    internal static async Task<int> ExecuteAsync(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        var result = await repository.ExecuteAsync(async connection => await connection.ExecuteAsync(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
        if (repository.DbType == DatabaseType.ClickHouse && result < 1)
        {
            return 1; // 保留 ClickHouse 原有的成功结果约定。
        }
        return result;
    }

    internal static async Task<IEnumerable<T>> QueryAsync<T>(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await repository.ExecuteAsync(async connection => await connection.QueryAsync<T>(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static async Task<T> GetAsync<T>(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await repository.ExecuteAsync(async connection => await connection.GetAsync<T>(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static async Task<T> ExecuteScalarAsync<T>(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await repository.ExecuteAsync(async connection => await connection.ExecuteScalarAsync<T>(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static async Task<object> ExecuteScalarAsync(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await repository.ExecuteAsync(async connection => await connection.ExecuteScalarAsync(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static async Task<DataTable> ExecuteDataTableAsync(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await repository.ExecuteAsync(async connection => await connection.ExecuteDataTableAsync(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static async Task<DataSet> ExecuteDataSetAsync(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        return await repository.ExecuteAsync(async connection => await connection.ExecuteDataSetAsync(sqlCommand, repository.Factory.SqlMonitor), sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection);
    }

    internal static async Task<IDataReader> ExecuteReaderAsync(BaseRepository repository, ISqlCommand sqlCommand)
    {
        if (sqlCommand == null) throw new ArgumentNullException(nameof(sqlCommand));

        // 只让 Reader 关闭仓储内部连接；连接已由通用执行方法打开，不能依赖 Dapper 自动推断。
        var behavior = sqlCommand.Transaction?.Connection == null && sqlCommand.Connection == null
            ? CommandBehavior.CloseConnection : CommandBehavior.Default;
        return await repository.ExecuteAsync(connection => connection.ExecuteReaderAsync(sqlCommand, repository.Factory.SqlMonitor, behavior),
            sqlCommand.Master, sqlCommand.Transaction, sqlCommand.Connection, autoDisposeInternalConnection: false);
    }
}
