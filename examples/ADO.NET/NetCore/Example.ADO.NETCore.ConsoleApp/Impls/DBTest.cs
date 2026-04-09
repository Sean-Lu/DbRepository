using Example.ADO.NETCore.Application.Contracts;
using Example.ADO.NETCore.ConsoleApp.Contracts;
using Microsoft.Extensions.Configuration;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.ConsoleApp.Impls;

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