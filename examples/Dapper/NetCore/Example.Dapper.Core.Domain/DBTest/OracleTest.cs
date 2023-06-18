#if UseOracle
using System;
using System.Data;
using Dapper;
using Example.Dapper.Core.Domain.Entities;
using Example.Dapper.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.Domain.DBTest
{
    public class OracleTest : ISimpleDo
    {
        private readonly IConfiguration _configuration;

        public OracleTest(IConfiguration configuration)
        {
            _configuration = configuration;//DIManager.GetService<IConfiguration>();
        }

        public void Execute()
        {
            using (var connection = new OracleConnection(_configuration.GetConnectionString("test_Oracle")))
            {
                connection.Open();
                var testEntity = new TestEntity
                {
                    UserId = 1,
                    UserName = "Test User",
                    Age = 28,
                    Sex = SexType.Male,
                    PhoneNumber = "123456789",
                    Email = "test@test.com",
                    IsVip = true,
                    IsBlack = false,
                    Country = CountryType.China,
                    AccountBalance = 1000.00m,
                    AccountBalance2 = 2000.00m,
                    Status = 1,
                    Remark = "Test Remark",
                    CreateTime = DateTime.Now
                };
                var sql = @"INSERT INTO ""Test"" (""UserId"", ""UserName"", ""Age"", ""Sex"", ""PhoneNumber"", ""Email"", ""IsVip"", ""IsBlack"", ""Country"", ""AccountBalance"", ""AccountBalance2"", ""Status"", ""Remark"", ""CreateTime"") 
                            VALUES (:UserId, :UserName, :Age, :Sex, :PhoneNumber, :Email, :IsVip, :IsBlack, :Country, :AccountBalance, :AccountBalance2, :Status, :Remark, :CreateTime) 
                            RETURNING ""Id"" INTO :Id";
                var parameters = new DynamicParameters(testEntity);
                //parameters.Add("Id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                var autoSetIdValue = true;// true: 自动赋值, false: 手动赋值
                if (autoSetIdValue)
                {
                    parameters.Output(testEntity, entity => entity.Id);
                }
                var insertResult = connection.Execute(sql, parameters);
                Console.WriteLine($"[Dapper]Insert result: {insertResult}");

                var id = parameters.Get<long>("Id");
                Console.WriteLine($"[Dapper]Insert return id: {id}");
                if (!autoSetIdValue)
                {
                    typeof(TestEntity).GetProperty(nameof(TestEntity.Id))?.SetValue(testEntity, id);
                }
                Console.WriteLine($"[Dapper]Entity id: {testEntity.Id}");
            }
        }
    }
}
#endif