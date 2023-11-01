using Example.EF.Core.ConsoleApp;
using Example.EF.Core.ConsoleApp.Contracts;
using Example.EF.Core.ConsoleApp.Entities;
using Example.EF.Core.ConsoleApp.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);// 设置当前工作目录：@".\"

#if UsePostgreSql || UseOpenGauss || UseHighgoDB || UseIvorySQL
// 解决 PostgreSQL 在使用 DateTime 类型抛出异常：Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#endif

#if UseInformix
// IBM Informix
Environment.SetEnvironmentVariable("IBM_DB_DIR", @"C:\Program Files\IBM Informix Client-SDK\bin");
#endif

//var connString = "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a";
//var optionsBuilder = new DbContextOptionsBuilder<EFDbContext>();
//optionsBuilder.UseMySQL(connString);

using (var db = new EFDbContext())
//using (var db = new EFDbContext(optionsBuilder.Options))
{
    if (db.Database.EnsureCreated())// CodeFirst: 自动创建表
    {
        Console.WriteLine("数据库创建并初始化完成...");
    }

    //db.Database.Migrate();

    var useRepository = false;

    if (!useRepository)
    {
        #region Using EF
        //var testModel = new TestUpperEntity
        var testModel = new TestEntity
        {
            //Id = 1,
            UserId = 10001,
            UserName = "Test",
            Age = 18,
            Country = CountryType.China,
            IsVip = true,
            Sex = SexType.Male,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,
            AccountBalance = 99.5M
        };

        db.TestEntities.Add(testModel);
        var addResult = db.SaveChanges() > 0;
        Console.WriteLine($"#################### Add 执行结果：{addResult}");

        var countResult = db.TestEntities.Count();
        //var countResult = db.Set<TestEntity>().Count();
        Console.WriteLine($"#################### Count 执行结果：{countResult}");

        var pageQueryResult = db.TestEntities
            .Where(entity => entity.Age >= 18 && entity.IsVip)
            .OrderBy(entity => entity.UserId)
            .ThenBy(entity => entity.Id)
            .Skip(0)
            .Take(3)
            .ToList();
        Console.WriteLine($"#################### Query 分页查询执行结果（有ORDER BY）：{JsonConvert.SerializeObject(pageQueryResult, Formatting.Indented)}");
        var pageQueryResultWithoutOrderBy = db.TestEntities
            .Where(entity => entity.Age >= 18 && entity.IsVip)
            .Skip(0)
            .Take(3)
            .ToList();
        Console.WriteLine($"#################### Query 分页查询执行结果（无ORDER BY）：{JsonConvert.SerializeObject(pageQueryResultWithoutOrderBy, Formatting.Indented)}");

        var getResult = db.TestEntities.Find(testModel.Id);
        Console.WriteLine($"#################### Get 执行结果：{JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

        getResult.AccountBalance += 100;
        db.TestEntities.Update(getResult);
        var updateResult = db.SaveChanges() > 0;
        Console.WriteLine($"#################### Update 执行结果：{updateResult}");

        db.TestEntities.Remove(getResult);
        var deleteResult = db.SaveChanges() > 0;
        //var deleteResult = testRepository.Delete(new TestEntity { Id = newModel.Id });
        Console.WriteLine($"#################### Delete 执行结果：{deleteResult}");
        #endregion
    }
    else
    {
        #region Using TableRepository
        //var testModel = new TestUpperEntity
        var testModel = new TestEntity
        {
            //Id = 1,
            UserId = 10001,
            UserName = "Test",
            Age = 18,
            Country = CountryType.China,
            IsVip = true,
            Sex = SexType.Male,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,
            AccountBalance = 99.5M
        };

        ITestRepository testRepository = new TestRepository(db);

        var addResult = testRepository.Add(testModel);
        Console.WriteLine($"#################### Add 执行结果：{addResult}");

        var countResult = testRepository.Count();
        Console.WriteLine($"#################### Count 执行结果：{countResult}");

        var queryResult = testRepository.QueryWithNoTracking(entity => entity.Age >= 18 && entity.IsVip && entity.Country == CountryType.China && entity.Sex == SexType.Male);
        Console.WriteLine($"#################### Query 执行结果：{JsonConvert.SerializeObject(queryResult, Formatting.Indented)}");

        var queryByDateTimeResult = testRepository.QueryWithNoTracking(entity => entity.CreateTime.Year == 2023 && entity.CreateTime.Month == 3);
        Console.WriteLine($"#################### Query 执行结果：{JsonConvert.SerializeObject(queryByDateTimeResult, Formatting.Indented)}");

        var getResult = testRepository.Get(entity => entity.Id == testModel.Id);
        Console.WriteLine($"#################### Get 执行结果：{JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

        getResult.AccountBalance += 100;
        var updateResult = testRepository.Update(getResult);
        Console.WriteLine($"#################### Update 执行结果：{updateResult}");

        getResult = testRepository.GetById(testModel.Id);
        Console.WriteLine($"#################### GetById 执行结果：{JsonConvert.SerializeObject(getResult, Formatting.Indented)}");

        var deleteResult = testRepository.Delete(getResult);
        //var deleteResult = testRepository.Delete(new TestEntity { Id = newModel.Id });
        Console.WriteLine($"#################### Delete 执行结果：{deleteResult}");
        #endregion
    }
}


Console.ReadLine();