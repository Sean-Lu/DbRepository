using Example.ADO.NETCore.Domain.Contracts;
using Example.ADO.NETCore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Sean.Core.DbRepository;
using Sean.Utility.Contracts;

namespace Example.ADO.NETCore.Domain.Repositories;

public class UserRepository : BaseRepository<UserEntity>, IUserRepository// Using ADO.NET
//public class UserRepository : DapperBaseRepository<UserEntity>, IUserRepository// Using Dapper
{
    private readonly ILogger _logger;

    public UserRepository(
        IConfiguration configuration,
        ISimpleLogger<UserRepository> logger
    ) : base(configuration)
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