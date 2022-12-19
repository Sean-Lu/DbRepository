using Example.EntityFramework;
using Example.EntityFramework.Entities;
using Example.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

//var connString = "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a";
//var optionsBuilder = new DbContextOptionsBuilder<EFDbContext>();
//optionsBuilder.UseMySQL(connString);

using (var db = new EFDbContext())
//using (var db = new EFDbContext(optionsBuilder.Options))
{
    //var countResult = db.TestEntities.Count();
    //Console.WriteLine($"#################### Count 执行结果：{countResult}");

    var testRepository = new TestRepository(db);

    var countResult2 = testRepository.Count();
    Console.WriteLine($"#################### Count 执行结果：{countResult2}");

    var queryResult = testRepository.Query(entity => entity.Id == 6);
    Console.WriteLine($"#################### Query 执行结果：{JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");
}


Console.ReadLine();