namespace Sean.Core.DbRepository.Test;

public abstract class DapperTestBase : TestBase
{
    protected DapperTestBase()
    {
        TestInfrastructure.EnsureInitialized();
    }
}
