using Example.ADO.NETCore.ConsoleApp.Contracts;
using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.DbFirst;

namespace Example.ADO.NETCore.ConsoleApp.Impls;

public class DbFirstTest(IConfiguration configuration) : ISimpleDo
{
    public void Execute()
    {
        Test();
    }

    private void Test()
    {
        var databaseType = DatabaseType.SQLite;
        var connString = configuration.GetConnectionString("test_SQLite");

        var codeGenerator = CodeGeneratorFactory.GetCodeGenerator(databaseType);
        codeGenerator.Initialize(connString);

        var tableName = "Test";
        var tableInfo = codeGenerator.GetTableInfo(tableName);
        var tableFieldInfo = codeGenerator.GetTableFieldInfo(tableName);
        var tableFieldReferenceInfo = codeGenerator.GetTableFieldReferenceInfo(tableName);
    }
}