﻿using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Extensions;

public static class SqlMonitorExtensions
{
    public static T Execute<T>(this ISqlMonitor sqlMonitor, IDbConnection connection, string sql, object sqlParameter, Func<T> func)
    {
        var timeWatcher = new Stopwatch();
        Exception exception = null;

        T result;
        try
        {
            if (sqlMonitor != null)
            {
                var sqlExecutingContext = new SqlExecutingContext(connection, sql, sqlParameter);
                sqlMonitor.OnSqlExecuting(sqlExecutingContext);
            }

            result = SynchronousWriteUtil.CheckWriteLock(DbContextConfiguration.Options.SynchronousWriteOptions, connection.ConnectionString, sql, () =>
            {
                timeWatcher.Restart();
                var funcResult = func();
                timeWatcher.Stop();
                return funcResult;
            });
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            if (sqlMonitor != null)
            {
                var sqlExecutedContext = new SqlExecutedContext(connection, sql, sqlParameter)
                {
                    ExecutionElapsed = timeWatcher.ElapsedMilliseconds,
                    Exception = exception
                };
                sqlMonitor.OnSqlExecuted(sqlExecutedContext);
            }
        }

        return result;
    }
    public static T Execute<T>(this ISqlMonitor sqlMonitor, DbCommand command, Func<T> func)
    {
        return sqlMonitor.Execute(command.Connection, command.CommandText, command.Parameters, func);
    }
    public static T Execute<T>(this ISqlMonitor sqlMonitor, IDbConnection connection, ISqlCommand sqlCommand, Func<T> func)
    {
        return sqlMonitor.Execute(connection, sqlCommand.Sql, sqlCommand.Parameter, func);
    }

    public static async Task<T> ExecuteAsync<T>(this ISqlMonitor sqlMonitor, IDbConnection connection, string sql, object sqlParameter, Func<Task<T>> func)
    {
        var timeWatcher = new Stopwatch();
        Exception exception = null;

        T result;
        try
        {
            if (sqlMonitor != null)
            {
                var sqlExecutingContext = new SqlExecutingContext(connection, sql, sqlParameter);
                sqlMonitor.OnSqlExecuting(sqlExecutingContext);
            }

            result = await SynchronousWriteUtil.CheckWriteLockAsync(DbContextConfiguration.Options.SynchronousWriteOptions, connection.ConnectionString, sql, async () =>
            {
                timeWatcher.Restart();
                var funcResult = await func();
                timeWatcher.Stop();
                return funcResult;
            });
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            if (sqlMonitor != null)
            {
                var sqlExecutedContext = new SqlExecutedContext(connection, sql, sqlParameter)
                {
                    ExecutionElapsed = timeWatcher.ElapsedMilliseconds,
                    Exception = exception
                };
                sqlMonitor.OnSqlExecuted(sqlExecutedContext);
            }
        }

        return result;
    }
    public static async Task<T> ExecuteAsync<T>(this ISqlMonitor sqlMonitor, DbCommand command, Func<Task<T>> func)
    {
        return await sqlMonitor.ExecuteAsync(command.Connection, command.CommandText, command.Parameters, func);
    }
    public static async Task<T> ExecuteAsync<T>(this ISqlMonitor sqlMonitor, IDbConnection connection, ISqlCommand sqlCommand, Func<Task<T>> func)
    {
        return await sqlMonitor.ExecuteAsync(connection, sqlCommand.Sql, sqlCommand.Parameter, func);
    }
}