#if UseQuestDB
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using Example.ADO.NETCore.Infrastructure;

namespace Example.ADO.NETCore.Domain.DBTest
{
    public class QuestDBTest
    {
        private readonly IConfiguration _configuration;

        public QuestDBTest()
        {
            _configuration = DIManager.GetService<IConfiguration>();
        }

        public void Execute()
        {
            // ****** QuestDB 数据库不支持 DELETE 删除操作 ******

            using (var conn = new NpgsqlConnection(_configuration.GetConnectionString("test_QuestDB")))
            {
                conn.Open();

                // 创建表
                var createTableSql = @"CREATE TABLE Test01 (
                                        Id BIGINT,
                                        Name TEXT,
                                        Age INT
                                     )";
                using (var createTableCmd = new NpgsqlCommand(createTableSql, conn))
                {
                    var createTableResult = createTableCmd.ExecuteNonQuery();
                }

                // 插入数据
                var insertSql = "INSERT INTO Test01 (Id, Name, Age) VALUES (@Id, @Name, @Age)";
                using (var insertCmd = new NpgsqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("Id", 1);
                    insertCmd.Parameters.AddWithValue("Name", "Alice");
                    insertCmd.Parameters.AddWithValue("Age", 20);
                    var insertResult = insertCmd.ExecuteNonQuery();
                }

                // 查询数据
                var selectSql = "SELECT * FROM Test01";
                using (var selectCmd = new NpgsqlCommand(selectSql, conn))
                using (var reader = selectCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("Id: " + reader.GetInt64(0));
                        Console.WriteLine("Name: " + reader.GetString(1));
                        Console.WriteLine("Age: " + reader.GetInt32(2));
                    }
                }

                // 删除表
                var dropTableSql = @"DROP TABLE Test01";
                using (var createTableCmd = new NpgsqlCommand(dropTableSql, conn))
                {
                    var dropTableResult = createTableCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
#endif