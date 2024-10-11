using Example.Dapper.Domain.Contracts;
using Example.Dapper.Model.Entities;
using Sean.Core.DbRepository.Dapper;
using Sean.Utility.Contracts;

namespace Example.Dapper.Domain.Repositories;

//public class UserRepository : BaseRepository<UserEntity>, IUserRepository// Using ADO.NET
public class UserRepository : DapperBaseRepository<UserEntity>, IUserRepository// Using Dapper
{
    private readonly ILogger _logger;

    public UserRepository(
        ISimpleLogger<UserRepository> logger
    ) : base()
    {
        _logger = logger;
    }

    public override string TableName()
    {
        var tableName = base.TableName();
        AutoCreateTable(tableName);// 自动创建表（如果表不存在）
        return tableName;
    }
}