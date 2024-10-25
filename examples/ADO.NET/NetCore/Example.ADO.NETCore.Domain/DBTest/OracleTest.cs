#if UseOracle
using System;
using System.Data;
using Example.ADO.NETCore.Domain.Entities;
using Example.ADO.NETCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace Example.ADO.NETCore.Domain.DBTest
{
    public class OracleTest
    {
        private readonly IConfiguration _configuration;

        public OracleTest()
        {
            _configuration = DIManager.GetService<IConfiguration>();
        }

        public void Execute()
        {
            using (var connection = new OracleConnection(_configuration.GetConnectionString("test_Oracle")))
            {
                connection.Open();
                var testEntity = new TestEntity();
                var sql = "INSERT INTO \"Test\" (\"UserId\", \"UserName\", \"Age\", \"Sex\", \"PhoneNumber\", \"Email\", \"IsVip\", \"IsBlack\", \"Country\", \"AccountBalance\", \"AccountBalance2\", \"Status\", \"Remark\", \"CreateTime\")" +
                                " VALUES (:UserId, :UserName, :Age, :Sex, :PhoneNumber, :Email, :IsVip, :IsBlack, :Country, :AccountBalance, :AccountBalance2, :Status, :Remark, :CreateTime)" +
                                " RETURNING \"Id\" INTO :Id";
                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("UserId", OracleDbType.Int64)).Value = 1;
                    command.Parameters.Add(new OracleParameter("UserName", OracleDbType.NVarchar2)).Value = "Test User";
                    command.Parameters.Add(new OracleParameter("Age", OracleDbType.Int32)).Value = 28;
                    command.Parameters.Add(new OracleParameter("Sex", OracleDbType.Int32)).Value = 1;
                    command.Parameters.Add(new OracleParameter("PhoneNumber", OracleDbType.NVarchar2)).Value = "123456789";
                    command.Parameters.Add(new OracleParameter("Email", OracleDbType.NVarchar2)).Value = "test@test.com";
                    command.Parameters.Add(new OracleParameter("IsVip", OracleDbType.Byte)).Value = 0;
                    command.Parameters.Add(new OracleParameter("IsBlack", OracleDbType.Byte)).Value = 0;
                    command.Parameters.Add(new OracleParameter("Country", OracleDbType.Int32)).Value = 1;
                    command.Parameters.Add(new OracleParameter("AccountBalance", OracleDbType.Decimal)).Value = 1000.00;
                    command.Parameters.Add(new OracleParameter("AccountBalance2", OracleDbType.Decimal)).Value = 2000.00;
                    command.Parameters.Add(new OracleParameter("Status", OracleDbType.Int32)).Value = 1;
                    command.Parameters.Add(new OracleParameter("Remark", OracleDbType.NVarchar2)).Value = "Test Remark";
                    command.Parameters.Add(new OracleParameter("CreateTime", OracleDbType.TimeStamp)).Value = DateTime.Now;

                    OracleParameter outputParameter = new OracleParameter();
                    outputParameter.ParameterName = "Id";
                    outputParameter.DbType = DbType.Int64;
                    //outputParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(outputParameter);
                    var autoSetIdValue = false;// true: 自动赋值, false: 手动赋值

                    var insertResult = command.ExecuteNonQuery();
                    Console.WriteLine($"[ADO.NET]Insert result: {insertResult}");

                    //var id = Convert.ToInt64(outputParameter.Value.ToString());
                    var id = Convert.ToInt64(command.Parameters["Id"].Value.ToString());
                    Console.WriteLine($"[ADO.NET]Insert return id: {id}");
                    if (!autoSetIdValue)
                    {
                        typeof(TestEntity).GetProperty(nameof(TestEntity.Id))?.SetValue(testEntity, id);
                    }
                    Console.WriteLine($"[ADO.NET]Entity id: {testEntity.Id}");
                }
            }
        }
    }
}
#endif