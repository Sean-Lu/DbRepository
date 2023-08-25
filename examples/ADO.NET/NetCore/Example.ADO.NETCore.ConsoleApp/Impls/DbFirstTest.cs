using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Core.DbRepository.DbFirst;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.ConsoleApp.Impls
{
    public class DbFirstTest : ISimpleDo
    {
        private readonly IConfiguration _configuration;

        public DbFirstTest(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Execute()
        {
            Test();
        }

        private void Test()
        {
            var databaseType = DatabaseType.SQLite;
            var connString = _configuration.GetConnectionString("test_SQLite");

            var codeGenerator = CodeGeneratorFactory.GetCodeGenerator(databaseType);
            codeGenerator.Initialize(connString);

            var tableName = "Test";
            var tableInfo = codeGenerator.GetTableInfo(tableName);
            var tableFieldInfo = codeGenerator.GetTableFieldInfo(tableName);
            var tableFieldReferenceInfo = codeGenerator.GetTableFieldReferenceInfo(tableName);
        }
    }
}
