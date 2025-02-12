﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Example.Common;
using Example.Dapper.Domain.Contracts;
using Example.Dapper.Model.Entities;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Dapper;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.Dapper.Domain.Repositories;

//public class TestRepository : BaseRepository<TestEntity>, ITestRepository// Using ADO.NET
public class TestRepository : DapperBaseRepository<TestEntity>, ITestRepository// Using Dapper
{
    private readonly ILogger _logger;

    public TestRepository(
        ILogger<TestRepository> logger
        //) : base()// MySQL: CRUD test passed.
        //) : base("test_MariaDB")// MariaDB: CRUD test passed.
        //) : base("test_TiDB")// TiDB: CRUD test passed.
        //) : base("test_OceanBase")// OceanBase: CRUD test passed.
        //) : base("test_SqlServer")// SQL Server: CRUD test passed.
        //) : base("test_Oracle")// Oracle: CRUD test passed.
        ) : base("test_SQLite")// SQLite: CRUD test passed.
        //) : base("test_DuckDB")// DuckDB: CRUD test passed.
        //) : base("test_MsAccess")// MS Access: CRUD test passed.
        //) : base("test_Firebird")// Firebird: CRUD test passed.
        //) : base("test_PostgreSql")// PostgreSql: CRUD test passed.
        //) : base("test_OpenGauss")// OpenGauss: CRUD test passed.
        //) : base("test_HighgoDB")// HighgoDB: CRUD test passed.
        //) : base("test_IvorySQL")// IvorySQL: CRUD test passed.
        //) : base("test_QuestDB")// QuestDB: CRUD test passed.
        //) : base("test_DB2")// DB2: CRUD test passed.
        //) : base("test_Informix")// Informix: CRUD test passed.
        //) : base("test_ClickHouse")// ClickHouse: CRUD test passed.
        //) : base("test_Dameng")// Dameng（达梦）: CRUD test passed.
        //) : base("test_KingbaseES")// KingbaseES（人大金仓）: CRUD test passed.
        //) : base("test_ShenTong")// ShenTong（神通数据库）: CRUD test passed.
        //) : base("test_Xugu")// Xugu（虚谷数据库）: CRUD test passed.
    {
        _logger = logger;

        //DbType = DatabaseType.MariaDB;
    }

    protected override void OnSqlExecuting(SqlExecutingContext context)
    {
        base.OnSqlExecuting(context);

        //_logger.LogInfo($"SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        //context.Handled = true;
    }

    protected override void OnSqlExecuted(SqlExecutedContext context)
    {
        base.OnSqlExecuted(context);

        if (context.Exception != null)
        {
            _logger.LogError($"SQL执行异常({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}{Environment.NewLine}{context.Exception}");
            context.Handled = true;
            return;
        }

        _logger.LogInfo($"SQL已经执行({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented)}");
        context.Handled = true;
    }

    public override string TableName()
    {
        var tableName = base.TableName();
        AutoCreateTable(tableName);// 自动创建表（如果表不存在）
        return tableName;
    }

    protected override IEnumerable<string> GetCreateTableSql(string tableName)
    {
        return base.GetCreateTableSql(tableName);

        //var sql = DbType switch
        //{
        //    DatabaseType.MySql => File.ReadAllText(@"./SQL/MySQL_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.MariaDB => File.ReadAllText(@"./SQL/MariaDB_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.TiDB => File.ReadAllText(@"./SQL/TiDB_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.OceanBase => File.ReadAllText(@"./SQL/OceanBase_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.SqlServer => File.ReadAllText(@"./SQL/SQLSever_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.Oracle => File.ReadAllText(@"./SQL/Oracle11g_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.SQLite => File.ReadAllText(@"./SQL/SQLite_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.DuckDB => File.ReadAllText(@"./SQL/DuckDB_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.MsAccess => File.ReadAllText(@"./SQL/MsAccess_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.Firebird => File.ReadAllText(@"./SQL/Firebird_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.PostgreSql => File.ReadAllText(@"./SQL/PostgreSql_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.OpenGauss => File.ReadAllText(@"./SQL/OpenGauss_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.HighgoDB => File.ReadAllText(@"./SQL/HighgoDB_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.IvorySQL => File.ReadAllText(@"./SQL/IvorySQL_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.QuestDB => File.ReadAllText(@"./SQL/QuestDB_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.DB2 => File.ReadAllText(@"./SQL/DB2_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.Informix => File.ReadAllText(@"./SQL/Informix_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.ClickHouse => File.ReadAllText(@"./SQL/ClickHouse_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.Dameng => File.ReadAllText(@"./SQL/Dameng_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.KingbaseES => File.ReadAllText(@"./SQL/KingbaseES_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.ShenTong => File.ReadAllText(@"./SQL/ShenTong_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    DatabaseType.Xugu => File.ReadAllText(@"./SQL/Xugu_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
        //    _ => throw new NotImplementedException()
        //};
        //return DbType is DatabaseType.DuckDB or DatabaseType.Firebird
        //    ? sql.Split(new[] { Constants.MultiSqlSeparatorComment }, StringSplitOptions.RemoveEmptyEntries).ToList()
        //    : new List<string> { sql };
    }

    public async Task<bool> TestCRUDAsync(IDbTransaction trans = null)
    {
        _logger.LogDebug($"######Current database type: {DbType}");

        var testModel = new TestEntity
        {
            //Id = 1,
            UserId = 10001,
            UserName = "Test01",
            Age = 18,
            IsVip = true,
            AccountBalance = 99.95M,
            Remark = "Test",
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };
        var addResult = await AddAsync(testModel, true, transaction: trans);
        _logger.LogDebug($"######Add result: {addResult}");
        if (!addResult)
        {
            return false;
        }

        testModel.AccountBalance = 1000M;
        testModel.Age = 20;
        var updateResult = await UpdateAsync(testModel, entity => new { entity.AccountBalance, entity.Age }, transaction: trans) > 0;
        _logger.LogDebug($"######Update result: {updateResult}");
        if (!updateResult)
        {
            return false;
        }

        if (DbType != DatabaseType.QuestDB) // QuestDB 数据库不支持 DELETE 删除操作
        {
            testModel.AccountBalance++;
            testModel.Age++;
            var addOrUpdateResult = await AddOrUpdateAsync(testModel, transaction: trans);
            _logger.LogDebug($"######AddOrUpdate result: {addOrUpdateResult}");
            if (!addOrUpdateResult)
            {
                return false;
            }
        }

        var incrResult = await IncrementAsync(1, entity => entity.AccountBalance, entity => entity.Id == testModel.Id, transaction: trans);
        _logger.LogDebug($"######Increment result: {incrResult}");
        if (!incrResult)
        {
            return false;
        }

        var orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.UserId);
        orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.Id);
        var queryResult = (await QueryAsync(entity => true, orderBy, 1, 3))?.ToList();
        _logger.LogDebug($"######Query result: {JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

        var getResult = await GetAsync(entity => entity.UserId == 10001);
        _logger.LogDebug($"######Get result: {JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

        var sqlCommand = this.CreateQueryableBuilder()
            .Where(entity => entity.Age >= 18 && entity.IsVip)
            .OrderBy(OrderByType.Desc, entity => entity.CreateTime)
            .Page(1, 3)
            .Build();
        var executeDataTableResult = await ExecuteDataTableAsync(sqlCommand);
        _logger.LogDebug($"######ExecuteDataTable result: {JsonConvert.SerializeObject(executeDataTableResult, Formatting.Indented)}");

        var sqlCommand2 = this.CreateQueryableBuilder()
            .SelectFields(entity => new { entity.Id, entity.UserId, entity.Email })
            .Where(entity => entity.Id == testModel.Id)
            .Build();
        //var getTupleData = await GetAsync<Tuple<long, long, string>>(sqlCommand2);// Tuple<>
        //_logger.LogDebug($"######GetTupleData result: {JsonConvert.SerializeObject(getTupleData, Formatting.Indented)}");
        var getValueTupleData = await GetAsync<(long, long, string)>(sqlCommand2);// 匿名类：ValueTuple<>
        _logger.LogDebug($"######GetValueTupleData result: {JsonConvert.SerializeObject(getValueTupleData, Formatting.Indented)}");
        var getDynamicData = await GetAsync<dynamic>(sqlCommand2);// dynamic动态类型
        _logger.LogDebug($"######GetDynamicData result: {JsonConvert.SerializeObject(getDynamicData, Formatting.Indented)}");

        if (DbType != DatabaseType.QuestDB) // QuestDB 数据库不支持 DELETE 删除操作
        {
            var deleteResult = await DeleteAsync(testModel, transaction: trans);
            _logger.LogDebug($"######Delete result: {deleteResult}");
            if (!deleteResult)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> TestCRUDWithTransactionAsync()
    {
        if (DbType == DatabaseType.SQLite)
        {
            TableName();// 如果表不存在，需要先创建表，否则下面的事务操作会在自动创建表的时候出现锁库的情况
        }

        var transResult = await ExecuteAutoTransactionAsync(async trans =>
        {
            if (!await TestCRUDAsync(trans))
            {
                return false;
            }

            return true;
        });

        _logger.LogDebug($"######Transaction result: {transResult}");
        return transResult;
    }

    private async Task TestUnionAllAsync()
    {
        // UNION ALL：合并所有分表数据
        var hexCount = 2;// 分表表名后缀16进制位数
        var sqlList = new List<string>();
        var tableCount = Math.Pow(16, hexCount);
        for (var i = 0; i < tableCount; i++)
        {
            sqlList.Add($"SELECT * FROM OrderExtUser_{Convert.ToString(i, 16).PadLeft(hexCount, '0').ToLower()}");
        }
        var sql = string.Join(" UNION ALL ", sqlList);
        var result = (await QueryAsync<dynamic>(sql))?.ToList();
    }
}