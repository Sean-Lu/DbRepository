using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Example.ADO.NETCore.Domain.Contracts;
using Example.ADO.NETCore.Domain.Entities;
using Example.ADO.NETCore.Infrastructure.Converter;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.Extensions;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.Domain.Repositories
{
    public class TestRepository : EntityBaseRepository<TestEntity>, ITestRepository// Using ADO.NET
    //public class TestRepository : BaseRepository<TestEntity>, ITestRepository// Using Dapper
    {
        private readonly ILogger _logger;

        public TestRepository(
            IConfiguration configuration,
            ISimpleLogger<TestRepository> logger
            //) : base(configuration)// MySQL: CRUD test passed.
            ) : base(configuration, "test_SQLite")// SQLite: CRUD test passed.
            //) : base(configuration, "test_SqlServer")// SqlServer
            //) : base(configuration, "test_Oracle")// Oracle
            //) : base(configuration, "test_PostgreSql")// PostgreSql
            //) : base(configuration, "test_Firebird")// Firebird
            //) : base(configuration, "test_Informix")// Informix
            //) : base(configuration, "test_DB2")// DB2
        {
            _logger = logger;
        }

        protected override void OnSqlExecuting(SqlExecutingContext context)
        {
            base.OnSqlExecuting(context);

            //_logger.LogInfo($"SQL准备执行: {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
            //context.Handled = true;
        }

        protected override void OnSqlExecuted(SqlExecutedContext context)
        {
            base.OnSqlExecuted(context);

            _logger.LogInfo($"SQL已经执行({context.ExecutionElapsed}ms): {context.Sql}{Environment.NewLine}参数：{JsonConvert.SerializeObject(context.SqlParameter, Formatting.Indented, new DbParameterCollectionConverter())}");
            context.Handled = true;
        }

        public override string TableName()
        {
            var tableName = base.TableName();
            AutoCreateTable(tableName);// 自动创建表（如果表不存在）
            return tableName;
        }

        public override string CreateTableSql(string tableName)
        {
            return DbType switch
            {
                DatabaseType.MySql => File.ReadAllText(@"./SQL/MySQL_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
                DatabaseType.SQLite => File.ReadAllText(@"./SQL/SQLite_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
                DatabaseType.SqlServer => File.ReadAllText(@"./SQL/SQLSever_CreateTable_Test.sql").Replace("{$TableName$}", tableName),
                _ => throw new NotImplementedException()
            };
        }

        public async Task<bool> TestCRUDAsync(IDbTransaction trans = null)
        {
            var testModel = new TestEntity
            {
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

            var incrResult = await IncrementAsync(1, entity => entity.AccountBalance, entity => entity.Id == testModel.Id, transaction: trans);
            _logger.LogDebug($"######Increment result: {incrResult}");
            if (!incrResult)
            {
                return false;
            }

            //var orderBy = new OrderByCondition($"{Table<TestEntity>.Field(entity => entity.UserId)} {OrderByType.Asc}");
            //orderBy.Next = new OrderByCondition($"{Table<TestEntity>.Field(entity => entity.Id)} {OrderByType.Asc}");
            var orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.UserId);
            orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.Id);
            var queryResult = (await QueryAsync(entity => true, orderBy, 1, 3))?.ToList();
            _logger.LogDebug($"######Query result: {JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

            var getResult = await GetAsync(entity => entity.UserId == 10001);
            _logger.LogDebug($"######Get result: {JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

            var sqlCommand = this.CreateQueryableBuilder(true)
                .Where(entity => entity.Age >= 18 && entity.IsVip)
                .Page(1, 3)
                .Build();
            var executeDataTableResult = await ExecuteDataTableAsync(sqlCommand);
            _logger.LogDebug($"######ExecuteDataTable result: {JsonConvert.SerializeObject(executeDataTableResult, Formatting.Indented)}");

            var deleteResult = await DeleteAsync(testModel, transaction: trans);
            _logger.LogDebug($"######Delete result: {deleteResult}");
            if (!deleteResult)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> TestCRUDWithTransactionAsync()
        {
            using (var trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (Transaction.Current != null)
                {
                    Transaction.Current.TransactionCompleted += (sender, args) =>
                    {
                        _logger.LogDebug("###################环境事务执行结束");
                    };
                }

                if (!await TestCRUDAsync())
                {
                    return false;
                }

                trans.Complete();
            }

            return true;
        }
    }
}
