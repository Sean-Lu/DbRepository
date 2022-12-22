using Example.EntityFramework;
using Example.EntityFramework.Contracts;
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

    ITestRepository testRepository = new TestRepository(db);

    var id = 6L;
    var newModel = new TestEntity
    {
        Id = id,
        UserId = 10001,
        UserName = "Test",
        Age = 18,
        Country = CountryType.China,
        IsVip = true,
        Sex = SexType.Male,
        CreateTime = DateTime.Now,
        AccountBalance = 99.5M
    };
    var addResult = testRepository.Add(newModel);
    Console.WriteLine($"#################### Add 执行结果：{addResult}");

    var countResult = testRepository.Count();
    Console.WriteLine($"#################### Count 执行结果：{countResult}");

    var queryResult = testRepository.QueryWithNoTracking(entity => entity.Age >= 18 && entity.IsVip);
    Console.WriteLine($"#################### Query 执行结果：{JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

    var getResult = testRepository.Get(entity => entity.Id == id);
    Console.WriteLine($"#################### Get 执行结果：{JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

    getResult.AccountBalance += 100;
    var updateResult = testRepository.Update(getResult);
    Console.WriteLine($"#################### Update 执行结果：{updateResult}");

    getResult = testRepository.GetById(id);
    Console.WriteLine($"#################### GetById 执行结果：{JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

    var deleteResult = testRepository.Delete(getResult);
    //var deleteResult = testRepository.Delete(new TestEntity { Id = id });
    Console.WriteLine($"#################### Delete 执行结果：{deleteResult}");
}


Console.ReadLine();