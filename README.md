## 简介

> `ORM`，支持所有**关系型数据库**（实现`DbProviderFactory`），如：`MySQL`、`SQL Server`、`Oracle`、`SQLite`、`Access`、`Firebird`、`PostgreSql`、`DB2`、`Informix`等

- 支持主从库分离（主库：增\删\改，从库：查）
- 支持分表
- 支持`Expression`表达式树（自动转换为参数化SQL语句）

```
注：非参数化SQL会有SQL注入的风险。
```

- 核心类：

| Class                                         | 说明                                                    |
| --------------------------------------------- | ----------------------------------------------------- |
| `BaseRepository<TEntity>`<br>`BaseRepository` | 抽象类，基于`DbFactory`+`Dapper`扩展，支持的数据库见枚举：`DatabaseType` |
| `DbFactory`                                   | 数据库工厂，支持所有关系型数据库（实现`DbProviderFactory`）               |
| `SqlFactory`                                  | `SQL`创建工厂（增\删\改\查\...）                                |
| `DbProviderFactoryManager`                    | `System.Data.Common.DbProviderFactory`                |

- `DbFactory`特别说明：

```
Get<T>()、GetList<T>() 其中 T ：
1. 支持自定义的Model实体模型（表字段映射匹配实体属性是否大小写敏感： DbFactory.CaseSensitiveWhenMatchField ）
2. 支持dynamic（动态类型）
2. 支持以下类型（默认只取查询结果的第一列数据）：
    - 值类型，如：int、long、double、decimal、DateTime、bool等
    - string字符串类型（特殊的引用类型）
3. 部分特殊类型暂时不支持，如：KeyValuePair<long, decimal>

注：Dapper不仅支持以上所有类型，兼容性和性能也更好，建议使用：BaseRepository<TEntity> + Dapper
```

- Oracle：

```
微软官方已经废弃System.Data.OracleClient（需要安装Oracle客户端），且不再更新。
推荐使用Oracle官方提供的数据库连接驱动：Oracle.ManagedDataAccess（不需要安装Oracle客户端）。
```

## Nuget Packages

| Package                                                                                        | NuGet Stable                                                                                                                                                        | NuGet Pre-release                                                                                                                                                      | Downloads                                                                                                                                                            |
| ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Sean.Core.DbRepository](https://www.nuget.org/packages/Sean.Core.DbRepository/)               | [![Sean.Core.DbRepository](https://img.shields.io/nuget/v/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      | [![Sean.Core.DbRepository](https://img.shields.io/nuget/vpre/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      | [![Sean.Core.DbRepository](https://img.shields.io/nuget/dt/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      |
| [Sean.Core.DbRepository.Dapper](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/v/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/vpre/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/dt/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) |

## 数据库连接字符串配置

- `.NET Framework`: `App.config`、`Web.config`

```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <connectionStrings>
        <!--主库：可以配置多个，后缀是以1开始的数字-->
        <add name="master" connectionString="DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a" providerName="MySql.Data.MySqlClient"/>
        <!--从库：可以配置多个，后缀是以1开始的数字-->
        <add name="secondary1" connectionString="DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a" providerName="MySql.Data.MySqlClient"/>
        <add name="secondary2" connectionString="DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a" providerName="MySql.Data.MySqlClient"/>
    </connectionStrings>
    <appSettings>

    </appSettings>
    <system.data>
        <DbProviderFactories>
            <remove invariant="MySql.Data.MySqlClient" />
            <add name="MySQL Data Provider" invariant="MySql.Data.MySqlClient" description=".Net Framework Data Provider for MySQL" type="MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data"/>
        </DbProviderFactories>
    </system.data>
</configuration>
```

- `.NET Core`: `appsettings.json`【**通过`ProviderName`或`DatabaseType`（枚举）来配置数据库客户端驱动**】

```
{
  "ConnectionStrings": {
    /*主库：可以配置多个，后缀是以1开始的数字*/
    "master": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",
    /*从库：可以配置多个，后缀是以1开始的数字*/
    "secondary1": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",
    "secondary2": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",

    "test_SqlServer": "server=127.0.0.1;database=test;uid=sa;pwd=123456!a;DatabaseType=SqlServer",
    "test_Oracle": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XXX)));User ID=XXX;Password=XXX;Persist Security Info=True;DatabaseType=Oracle",
    "test_SQLite": "data source=D:\\XXX.db;version=3;DatabaseType=SQLite"
  }
}
```

## 默认数据库驱动配置

> 配置文件：`dllconfigs\Sean.Core.DbRepository.dll.config`

```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <configSections>
        <section name="dbProviderMap" type="Sean.Core.DbRepository.DbProviderMapSection, Sean.Core.DbRepository" />
    </configSections>
    <dbProviderMap>
        <databases>
            <!-- Note: Only support relational database -->
            <database name="MySql" providerInvariantName="MySql.Data.MySqlClient" factoryTypeAssemblyQualifiedName="MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"/>
            <database name="SqlServer" providerInvariantName="System.Data.SqlClient" factoryTypeAssemblyQualifiedName="System.Data.SqlClient.SqlClientFactory,System.Data"/>
            <database name="SqlServerCe" providerInvariantName="Microsoft.SqlServerCe.Client" factoryTypeAssemblyQualifiedName="Microsoft.SqlServerCe.Client.SqlCeClientFactory,Microsoft.SqlServerCe.Client"/>
            <database name="Oracle" providerInvariantName="Oracle.ManagedDataAccess.Client" factoryTypeAssemblyQualifiedName="Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess"/>
            <database name="SQLite" providerInvariantName="System.Data.SQLite" factoryTypeAssemblyQualifiedName="System.Data.SQLite.SQLiteFactory,System.Data.SQLite"/>
            <database name="Access" providerInvariantName="System.Data.OleDb" factoryTypeAssemblyQualifiedName="System.Data.OleDb.OleDbFactory,System.Data"/>
            <database name="Firebird" providerInvariantName="FirebirdSql.Data.FirebirdClient" factoryTypeAssemblyQualifiedName="FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,FirebirdSql.Data.FirebirdClient"/>
            <database name="PostgreSql" providerInvariantName="Npgsql" factoryTypeAssemblyQualifiedName="Npgsql.NpgsqlFactory,Npgsql"/>
            <database name="DB2" providerInvariantName="IBM.Data.DB2" factoryTypeAssemblyQualifiedName="IBM.Data.DB2.Core.DB2Factory,IBM.Data.DB2.Core"/>
            <database name="Informix" providerInvariantName="IBM.Data.Informix" factoryTypeAssemblyQualifiedName="IBM.Data.Informix.IfxFactory,IBM.Data.Informix"/>
        </databases>
    </dbProviderMap>
</configuration>
```

## 性能对比（Performance Comparison）

> `Dapper`的`Execute`方法执行插入语句的本质是一条一条的插入，当数据量非常大时会很慢，可以把多条数据拼成一条脚本一次性执行。

- 以下测试结果来自单元测试：**`PerformanceComparisonTest.CompareBulkInsertTimeConsumed`**
- 测试数据库：MySQL 8.0.27
- 测试表：Test

| Operations       | 50 Entities | 200 Entities | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| ---------------- | ----------- | ------------ | -------------- | -------------- | -------------- |
| `Dapper.Execute` | 446 ms      | 1370 ms      | 6639 ms        | 12165 ms       | 31318 ms       |
| `BulkInsert`     | 10 ms       | 34 ms        | 437 ms         | 1674 ms        | 15062 ms       |

## 使用示例（Example）

> 项目：`examples\Example.NetCore`
> 
> 项目：`examples\Example.NetFramework`

- 表达式树：**`Expression<Func<TEntity, object>> fieldExpression`**

```csharp
// 单个字段：
entity => entity.Status

// 多个字段（匿名类型）：
entity => new { entity.Status, entity.UpdateTime }

// 更多使用示例在单元测试中：Sean.Core.DbRepository.Test.FieldExpressionTest
```

- 表达式树：**`Expression<Func<TEntity, bool>> whereExpression`**

```csharp
// 常量
entity => entity.UserId == 10001L

// 变量
entity => entity.UserId == _model.UserId

// bool
entity => entity.IsVip

// bool
entity => !entity.IsVip

// &&
entity => entity.UserId == _model.UserId && entity.AccountBalance < accountBalance

// ||
entity => entity.UserId == _model.UserId || entity.AccountBalance >= accountBalance

// StartsWith
entity => entity.UserId == _model.UserId && entity.Remark.StartsWith("测试")

// 更多使用示例在单元测试中：Sean.Core.DbRepository.Test.WhereExpressionTest
```

- 常用`TableEntity`实体类注解：

| Attribute                    | AttributeUsage | Namespace                                      | Description                          |
| ---------------------------- | -------------- | ---------------------------------------------- | ------------------------------------ |
| `TableAttribute`             | Class          | `System.ComponentModel.DataAnnotations.Schema` | 自定义表名                                |
| `KeyAttribute`               | Property       | `System.ComponentModel.DataAnnotations`        | 标记为主键字段                              |
| `DatabaseGeneratedAttribute` | Property       | `System.ComponentModel.DataAnnotations.Schema` | 设置数据库生成字段值的方式（通常和`KeyAttribute`一起使用） |
| `ColumnAttribute`            | Property       | `System.ComponentModel.DataAnnotations.Schema` | 自定义字段名                               |
| `NotMappedAttribute`         | Property       | `System.ComponentModel.DataAnnotations.Schema` | 标记为为忽略字段                             |
| ~~`IgnoreAttribute`~~        | Property       | `Sean.Core.DbRepository`                       | 标记为为忽略字段                             |
| ~~`ForeignKeyAttribute`~~    | Property       | `System.ComponentModel.DataAnnotations.Schema` | 标记为外键字段（***暂不支持***）                  |
| `SequenceAttribute`          | Class          | `Sean.Core.DbRepository`                       | Oracle: Sequence（生成自增Id）             |

## 常见问题

> 注：从`.NET Standard` 2.1版本开始（`.NET Core` >= 3.0）才有`System.Data.Common.DbProviderFactories`

- 报错：System.ArgumentException:“The specified invariant name 'MySql.Data.MySqlClient' wasn't found in the list of registered .NET Data Providers.”

```
.NET Core的数据库连接与.NET Framework略有不同。
在.NET Framework中，程序可用的数据库驱动程序在整个系统范围内自动可用（通过操作系统的machine.config或项目里的App.config、Web.config）。
而在.NET Core中，必须要先注册数据库工厂，示例：

SQLServer：
=========================================================
using System.Data.SqlClient;
DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
=========================================================

MySQL：
=========================================================
using MySql.Data.MySqlClient;
DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySqlClientFactory.Instance);
=========================================================

PostgreSQL：
=========================================================
using Npgsql;
DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);
=========================================================

SQLite：
=========================================================
using Microsoft.Data.Sqlite;
DbProviderFactories.RegisterFactory("Microsoft.Data.Sqlite", SqliteFactory.Instance);
=========================================================

这些数据库访问工厂的单例都是继承DbProviderFactory，需要通过nuget安装对应的数据库客户端包，例如：Mysql.Data
上面是通过直接注册工厂单例实现，也可以通过注册指定的工厂类型和应用程序集来实现，示例：
DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data");
然后就可以正常使用了：
DbProviderFactory factory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
DbConnection conn = factory.CreateConnection();
```
