using Example.Dapper.Core.Application.Contracts;
using Example.Dapper.Core.ConsoleApp.Contracts;
using Microsoft.Extensions.Configuration;
using Sean.Utility.Contracts;

namespace Example.Dapper.Core.ConsoleApp.Impls;

public class DBTest(
    ISimpleLogger<DBTest> logger,
    IConfiguration configuration,
    //ITestSimpleService testSimpleService,
    ITestService testService
    ) : ISimpleDo
{
    private readonly ILogger _logger = logger;

    public void Execute()
    {
        //testSimpleService.TestCRUDAsync().Wait();

        testService.TestCRUDAsync().Wait();
        //testService.TestCRUDWithTransactionAsync().Wait();
    }
}