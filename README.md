## 🌈 简介

> `ORM`框架，支持数据库：[`MySQL`](https://www.mysql.com/)、[`MariaDB`](https://mariadb.org/)、[`TiDB`](https://pingcap.com/)、[`OceanBase`](https://open.oceanbase.com/)、[`SQL Server`](https://www.microsoft.com/)、[`Oracle`](https://www.oracle.com/)、[`SQLite`](https://www.sqlite.org/)、[`DuckDB`](https://duckdb.org/)、[`MS Access`](https://www.microsoft.com/)、[`Firebird`](https://www.firebirdsql.org/)、[`PostgreSql`](https://www.postgresql.org/)、[`OpenGauss`](https://opengauss.org/)、[`HighgoDB(瀚高)`](https://www.highgo.com/)、[`IvorySQL`](https://ivorysql.org/)、[`QuestDB`](https://questdb.io/)、[`DB2`](https://www.ibm.com/)、[`Informix`](https://www.ibm.com/)、[`ClickHouse`](https://clickhouse.com/)、[`Dameng(达梦)`](https://www.dameng.com/)、[`KingbaseES(人大金仓)`](https://www.kingbase.com.cn/)、[`ShenTong(神通)`](http://www.shentongdata.com/)、[`Xugu(虚谷)`](http://www.xugucn.com/)

- 支持：`DbFirst`、`CodeFirst`
- 支持主从库分离（主库：增\删\改，从库：查）
- 支持分表（自定义表名规则）
- 支持`Expression`表达式树解析：`whereExpression`、`fieldExpression`
- 常用类：

| Class                                                          | Namespace                       | Description                    |
| -------------------------------------------------------------- | ------------------------------- | ------------------------------ |
| `DbFactory`                                                    | `Sean.Core.DbRepository`        | 数据库工厂                     |
| `SqlFactory`                                                   | `Sean.Core.DbRepository`        | `SQL`创建工厂（CRUD）          |
| **`BaseRepository`<br>`BaseRepository<TEntity>`**             | `Sean.Core.DbRepository`        | 基于`DbFactory`实现            |
| **`DapperBaseRepository`<br>`DapperBaseRepository<TEntity>`** | `Sean.Core.DbRepository.Dapper` | 基于`DbFactory`+`Dapper`实现   |

## ⭐ 开源

- GitHub: https://github.com/Sean-Lu/DbRepository
- Gitee: https://gitee.com/Sean-Lu/DbRepository

## 💖 Nuget Packages

| Package                                                                                        | NuGet Stable                                                                                                                                                        | NuGet Pre-release                                                                                                                                                      | Downloads                                                                                                                                                            |
| ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Sean.Core.DbRepository](https://www.nuget.org/packages/Sean.Core.DbRepository/)               | [![Sean.Core.DbRepository](https://img.shields.io/nuget/v/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      | [![Sean.Core.DbRepository](https://img.shields.io/nuget/vpre/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      | [![Sean.Core.DbRepository](https://img.shields.io/nuget/dt/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      |
| [Sean.Core.DbRepository.Dapper](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/v/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/vpre/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/dt/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) |

## 🍉 数据库

> CRUD Test: `TestRepository.cs`
>
> DbFirst: `CodeGeneratorFactory`
>
> CodeFirst: `SqlGeneratorFactory`

| Database     | CRUD Test | DbFirst | CodeFirst | Description |
| ------------ | --------- | ------- | --------- |------------ |
| `MySQL`      | ✅       | ✅      | ✅       |             |
| `MariaDB`    | ✅       | ✅      | ✅       |             |
| `TiDB`       | ✅       | ✅      | ✅       |             |
| `OceanBase`  | ✅       | ✅      | ✅       |             |
| `SQL Server` | ✅       | ✅      | ✅       |             |
| `Oracle`     | ✅       | ✅      | ✅       |             |
| `SQLite`     | ✅       | ✅      | ✅       |             |
| `DuckDB`     | ✅       | ✅      | ✅       |             |
| `MS Access`  | ✅       | ✅      | ✅       |             |
| `Firebird`   | ✅       | ✅      | ✅       |             |
| `PostgreSql` | ✅       | ✅      | ✅       |             |
| `OpenGauss`  | ✅       | ✅      | ✅       |             |
| `HighgoDB`   | ✅       | ✅      | ✅       | 瀚高数据库  |
| `IvorySQL`   | ✅       | ✅      | ✅       |             |
| `QuestDB`    | ✅       |         | ✅       |              |
| `DB2`        | ✅       |         | ✅       |              |
| `Informix`   | ✅       |         | ✅       |              |
| `ClickHouse` | ✅       |         | ✅       |              |
| `Dameng`     | ✅       | ✅      | ✅       | 达梦         |
| `KingbaseES` | ✅       |         | ✅       | 人大金仓     |
| `ShenTong`   | ✅       |         | ✅       | 神通数据库   |
| `Xugu`       | ✅       |         | ✅       | 虚谷数据库   |

## 💯 性能测试

> `Dapper`的`Execute`方法执行插入批量实体数据的本质是一条一条的插入，当数据量非常大时会很慢，可以分批把多条实体数据拼成一条脚本一次性执行（`BulkInsert`）。

- 以下测试结果来自单元测试：**`PerformanceComparisonTest.CompareBulkInsertTimeConsumed`**
- 测试数据库：MySQL 8.0.27
- 测试表：Test
- 测试时间：2023-02-07 15:00:00

| Operation        | 50 Entities | 200 Entities | 1000 Entities  | 2000 Entities  | 5000 Entities  |
| ---------------- | ----------- | ------------ | -------------- | -------------- | -------------- |
| `Dapper.Execute` | 318 ms      | 1401 ms      | 5875 ms        | 11991 ms       | 29968 ms       |
| `BulkInsert`     | 15 ms       | 27 ms        | 84 ms          | 176 ms         | 471 ms         |

## 👉 使用示例

### 数据库连接字符串配置

> `.NET Framework`: `App.config`、`Web.config`

- 配置示例：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <connectionStrings>
        <!-- 主库：如果配置了多个数据库，数据库名称后缀是以1开始的数字。 -->
        <add name="master" connectionString="DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a" providerName="MySql.Data.MySqlClient"/>
        <!-- 从库：如果配置了多个数据库，数据库名称后缀是以1开始的数字。 -->
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

> `.NET Core`: `appsettings.json`

- 配置示例：可以通过设置`ProviderName`或`DatabaseType`的值来指定数据库类型

```json
{
  "ConnectionStrings": {
    // 主库：如果配置了多个数据库，数据库名称后缀是以1开始的数字。
    "master": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",
    // 从库：如果配置了多个数据库，数据库名称后缀是以1开始的数字。
    "secondary1": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",
    "secondary2": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",

    "test_SqlServer": "server=127.0.0.1;database=test;uid=sa;pwd=123456!a;DatabaseType=SqlServer",
    "test_Oracle": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XXX)));User ID=XXX;Password=XXX;Persist Security Info=True;DatabaseType=Oracle",
    "test_SQLite": "data source=.\\test.db;pooling=True;busytimeout=30000;journal mode=Wal;DatabaseType=SQLite"
  }
}
```

### 数据库提供者工厂配置

> 支持2种方式来配置数据库和数据库提供者工厂之间的映射关系：

- 方式1：通过代码实现
- 方式2：通过配置文件实现

#### 方式1：代码

- 代码示例1：

```csharp
DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", MySqlClientFactory.Instance));// MySql
DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", SqlClientFactory.Instance));// Microsoft SQL Server
DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", OracleClientFactory.Instance));// Oracle
DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", SQLiteFactory.Instance));// SQLite
```

- 代码示例2：

```csharp
DatabaseType.MySql.SetDbProviderMap(new DbProviderMap("MySql.Data.MySqlClient", "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"));// MySql
DatabaseType.SqlServer.SetDbProviderMap(new DbProviderMap("System.Data.SqlClient", "System.Data.SqlClient.SqlClientFactory,System.Data.SqlClient"));// Microsoft SQL Server
DatabaseType.Oracle.SetDbProviderMap(new DbProviderMap("Oracle.ManagedDataAccess.Client", "Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess"));// Oracle
DatabaseType.SQLite.SetDbProviderMap(new DbProviderMap("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory,System.Data.SQLite"));// SQLite
```

- 代码示例3：

```csharp
// 如果直接使用数据库提供者工厂，也可以不配置数据库和数据库提供者工厂之间的映射关系。代码示例：
var db = new DbFactory("Database connection string...", MySqlClientFactory.Instance);// MySql
```

#### 方式2：配置文件

> 配置文件路径可以通过`DbContextConfiguration.Options.DbProviderFactoryConfigurationPath`设置

- 配置文件示例：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <configSections>
    <section name="dbProviderMap" type="Sean.Core.DbRepository.DbProviderMapSection, Sean.Core.DbRepository" />
  </configSections>
  <dbProviderMap>
    <databases>
      <!-- Supports configuring databases that implement the DbProviderFactory class. -->
      <database name="MySql" providerInvariantName="MySql.Data.MySqlClient" factoryTypeAssemblyQualifiedName="MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"/>
      <database name="MariaDB" providerInvariantName="MySqlConnector.MariaDB" factoryTypeAssemblyQualifiedName="MySqlConnector.MySqlConnectorFactory,MySqlConnector"/>
      <database name="TiDB" providerInvariantName="TiDB" factoryTypeAssemblyQualifiedName="MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"/>
      <database name="OceanBase" providerInvariantName="OceanBase" factoryTypeAssemblyQualifiedName="MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data"/>
      <database name="SqlServer" providerInvariantName="System.Data.SqlClient" factoryTypeAssemblyQualifiedName="System.Data.SqlClient.SqlClientFactory,System.Data"/>
      <database name="Oracle" providerInvariantName="Oracle.ManagedDataAccess.Client" factoryTypeAssemblyQualifiedName="Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess"/>
      <database name="SQLite" providerInvariantName="System.Data.SQLite" factoryTypeAssemblyQualifiedName="System.Data.SQLite.SQLiteFactory,System.Data.SQLite"/>
      <database name="DuckDB" providerInvariantName="DuckDB.NET.Data" factoryTypeAssemblyQualifiedName="DuckDB.NET.Data.DuckDBClientFactory,DuckDB.NET.Data"/>
      <database name="MsAccess" providerInvariantName="System.Data.OleDb" factoryTypeAssemblyQualifiedName="System.Data.OleDb.OleDbFactory,System.Data"/>
      <!--<database name="MsAccess" providerInvariantName="System.Data.Odbc" factoryTypeAssemblyQualifiedName="System.Data.Odbc.OdbcFactory,System.Data"/>-->
      <database name="Firebird" providerInvariantName="FirebirdSql.Data.FirebirdClient" factoryTypeAssemblyQualifiedName="FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,FirebirdSql.Data.FirebirdClient"/>
      <database name="PostgreSql" providerInvariantName="Npgsql" factoryTypeAssemblyQualifiedName="Npgsql.NpgsqlFactory,Npgsql"/>
      <database name="OpenGauss" providerInvariantName="OpenGauss" factoryTypeAssemblyQualifiedName="OpenGauss.NET.OpenGaussFactory,OpenGauss.NET"/>
      <database name="HighgoDB" providerInvariantName="HighgoDB" factoryTypeAssemblyQualifiedName="Npgsql.NpgsqlFactory,Npgsql"/>
      <database name="IvorySQL" providerInvariantName="IvorySQL" factoryTypeAssemblyQualifiedName="Npgsql.NpgsqlFactory,Npgsql"/>
      <database name="QuestDB" providerInvariantName="QuestDB" factoryTypeAssemblyQualifiedName="Npgsql.NpgsqlFactory,Npgsql"/>
      <database name="DB2" providerInvariantName="IBM.Data.DB2" factoryTypeAssemblyQualifiedName="IBM.Data.DB2.Core.DB2Factory,IBM.Data.DB2.Core"/>
      <database name="Informix" providerInvariantName="IBM.Data.Informix" factoryTypeAssemblyQualifiedName="IBM.Data.Informix.IfxFactory,IBM.Data.Informix"/>
      <database name="ClickHouse" providerInvariantName="ClickHouse.Client" factoryTypeAssemblyQualifiedName="ClickHouse.Client.ADO.ClickHouseConnectionFactory,ClickHouse.Client"/>
      <database name="Dameng" providerInvariantName="Dameng" factoryTypeAssemblyQualifiedName="Dm.DmClientFactory,DmProvider"/>
      <database name="KingbaseES" providerInvariantName="Kdbndp" factoryTypeAssemblyQualifiedName="Kdbndp.KdbndpFactory,Kdbndp"/>
      <database name="ShenTong" providerInvariantName="ShenTong" factoryTypeAssemblyQualifiedName="System.Data.OscarClient.OscarFactory,Oscar.Data.SqlClient"/>
      <database name="Xugu" providerInvariantName="Xugu" factoryTypeAssemblyQualifiedName="XuguClient.XGProviderFactory,XuguClient"/>
    </databases>
  </dbProviderMap>
</configuration>
```

### 常用实体类特性

> 数据库表实体类：`{Table}Entity`

| Attribute                                                                      | Use      | Description                                                    |
| ------------------------------------------------------------------------------ | -------- | -------------------------------------------------------------- |
| `Sean.Core.DbRepository.CodeFirstAttribute`                                    | Class    | 标记为CodeFirst的类                                            |
| **`System.ComponentModel.DataAnnotations.Schema.TableAttribute`**             | Class    | 设置表名                                                       |
| `Sean.Core.DbRepository.SequenceAttribute`                                     | Property | 设置序列号名称<br>（生成自增Id）                               |
| **`System.ComponentModel.DataAnnotations.KeyAttribute`**                      | Property | 标记为主键字段                                                 |
| **`System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute`** | Property | 设置数据库生成字段值的方式<br>（通常和`KeyAttribute`一起使用） |
| **`System.ComponentModel.DataAnnotations.Schema.ColumnAttribute`**            | Property | 设置字段名                                                     |
| `System.ComponentModel.DescriptionAttribute`                                   | Property | 设置字段描述                                                   |
| `Sean.Core.DbRepository.NumericAttribute`                                      | Property | 设置数值字段的位数和精度                                       |
| `System.ComponentModel.DataAnnotations.MaxLengthAttribute`                     | Property | 设置字段的最大长度                                             |
| `System.ComponentModel.DataAnnotations.RequiredAttribute`                      | Property | 设置字段不允许为空                                             |
| `System.ComponentModel.DefaultValueAttribute`                                  | Property | 设置字段默认值                                                 |
| **`System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute`**         | Property | 标记为为忽略字段                                               |
| ~~`System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute`~~         | Property | 标记为外键字段（***暂不支持***）                              |

### 增删改查示例

#### 仓储模式

> 仓储模式`CRUD`示例: `{Table}Repository`

```csharp
// Using ADO.NET
public class {Table}Repository : BaseRepository<{Table}Entity>, I{Table}Repository
// Using Dapper
public class {Table}Repository : DapperBaseRepository<{Table}Entity>, I{Table}Repository

// Table DTO
public class TestDto : DtoBase
{
    // ...
}

// Table Entity
public class TestEntity : EntityBase
{
    // ...
}

TestDto testDto = new TestDto();
// ...
TestEntity entity = mapper.Map<TestEntity>(testDto);

// 更多使用示例在单元测试中：Sean.Core.DbRepository.Test.TableRepositoryTest
```

##### 新增数据示例

```csharp
// 新增数据：
testRepository.Add(entity);

// 批量新增数据：
testRepository.Add(entities);

// 新增或更新数据：
testRepository.AddOrUpdate(entity);

// 批量新增或更新数据：
testRepository.AddOrUpdate(entities);
```

##### 删除数据示例

```csharp
// 删除数据：过滤条件默认为实体的主键字段
testRepository.Delete(entity);

// 删除数据：自定义过滤条件
testRepository.Delete(entity => entity.UserId == 10001 && entity.Status != 0);

// 删除全部数据：
testRepository.Delete(entity => true);

// 删除全部数据：
testRepository.DeleteAll();
```

##### 更新数据示例

```csharp
// 更新数据：更新全部字段，过滤条件默认为实体的主键字段
testRepository.Update(entity);

// 更新数据：更新部分字段，过滤条件默认为实体的主键字段
testRepository.Update(entity, fieldExpression: entity => new { entity.Status, entity.UpdateTime });

// 更新数据：更新全部字段，自定义过滤条件
testRepository.Update(entity, whereExpression: entity => entity.UserId == 10001 && entity.Status != 0);

// 更新数据：更新部分字段，自定义过滤条件
testRepository.Update(entity, fieldExpression: entity => new { entity.Status, entity.UpdateTime }, whereExpression: entity => entity.UserId == 10001 && entity.Status != 0);

// 更新数据：只更新 TDto 有的字段（内部默认映射）
testRepository.UpdateByDto(testDto);

// 更新数据：只更新 TDto 有的字段（外部自定义映射）
testRepository.UpdateByDto(testDto, mapper.Map<TestEntity>);

// 更新数据：只更新 TDto 有的字段 (外部传 Entity，通过 TDto 限定字段)
testRepository.UpdateByDto<TestDto>(entity);

// 批量更新数据：更新全部字段，过滤条件默认为实体的主键字段
testRepository.Update(entities);

// 批量更新数据：更新部分字段，过滤条件默认为实体的主键字段
testRepository.Update(entities, fieldExpression: entity => new { entity.Status, entity.UpdateTime });

// 批量更新数据：只更新 TDto 有的字段（内部默认映射）
testRepository.UpdateByDto(testDtos);

// 批量更新数据：只更新 TDto 有的字段（外部自定义映射）
testRepository.UpdateByDto(testDtos, mapper.Map<TestEntity>);

// 批量更新数据：只更新 TDto 有的字段 (外部传 Entity 集合，通过 TDto 限定字段)
testRepository.UpdateByDto<TestDto>(entities);

// 更新数值字段（增加）：
testRepository.Increment(10.0M, fieldExpression: entity => entity.AccountBalance, whereExpression: entity => entity.Id == 10001);

// 更新数值字段（减少）：
testRepository.Decrement(10.0M, fieldExpression: entity => entity.AccountBalance, whereExpression: entity => entity.Id == 10001);
```

##### 查询数据示例

```csharp
// 查询数据：分页 + 排序
int pageNumber = 1;// 当前页号（最小值为1）
int pageSize = 10;// 页大小
OrderByCondition orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.CreateTime);
orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.Id);
List<TestEntity> queryResult = testRepository.Query(entity => entity.UserId == 10001, orderBy, pageNumber, pageSize)?.ToList();

// 查询单个数据：
TestEntity getResult = testRepository.Get(entity => entity.Id == 2);

// 统计数量：
int countResult = testRepository.Count(entity => entity.UserId == 10001);

// 数据是否存在：
bool exists = testRepository.Exists(entity => entity.UserId == 10001);
```

##### WhereExpression示例

> 表达式树：`Expression<Func<TEntity, bool>> whereExpression`

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

##### FieldExpression示例

> 表达式树：`Expression<Func<TEntity, object>> fieldExpression`

```csharp
// 单个字段：
entity => entity.Status

// 多个字段（匿名类型）：
entity => new { entity.Status, entity.UpdateTime }

// 更多使用示例在单元测试中：Sean.Core.DbRepository.Test.FieldExpressionTest
```

## ❓ 常见问题

> `OleDb`和`ODBC`的区别？

1. `OleDb`是Microsoft开发的一种数据库连接技术，它是面向对象的，可以连接多种类型的数据库，包括`Access`、`Excel`、`SQL Server`等等。OleDb使用COM接口连接数据库，因此只能在Windows平台上使用。
2. `ODBC`是一种通用的数据库连接技术，它可以连接多种类型的数据库，包括`Access`、`Excel`、`SQL Server`等等。ODBC使用标准的API连接数据库，因此可以在多个平台上使用，包括Windows、Linux、Unix等等。

> 怎么解决`SQLite`数据库并发读写导致的锁库问题（database is locked）？

```
问：为什么SQLite并发读写容易出现锁库问题？
答：SQLite使用文件级锁定来管理对数据库的并发访问。这意味着在同一时间，只有一个写操作可以进行，而多个读操作可以并发进行。

SQLite数据库的WAL模式是一种日志机制，它可以减少写入操作时的锁冲突，从而提高并发性能。有以下2种方式启用WAL模式：
```

方式1：在连接字符串中添加：journal mode=Wal

```csharp
using System.Data.SQLite;

// 数据库连接字符串
var connStringBuilder = new SQLiteConnectionStringBuilder
{
    DataSource = @".\test.db",
    Pooling = true,
    BusyTimeout = 30000,
    JournalMode = SQLiteJournalModeEnum.Wal
};
var connString = connStringBuilder.ConnectionString;// data source=.\test.db;pooling=True;busytimeout=30000;journal mode=Wal
```

方式2：通过执行SQL命令来设置WAL模式：

```sql
-- 查看 SQLite 数据库日志模式：
PRAGMA journal_mode;
-- 设置 SQLite 数据库日志模式：
PRAGMA journal_mode = 'wal';
-- 查看 WAL 文件中未被同步的页面数：
PRAGMA wal_checkpoint;
```

- 需要注意的是，如果在多个线程或进程中对同一个数据库文件进行写入，且没有适当的同步机制，那么即使使用WAL模式，也可能会出现锁库的情况。使用WAL模式，读和写可以完全地并发执行，不会互相阻塞（但是写之间仍然不能并发）。为了解决并发写入问题，需要增加如下配置（同步锁机制）：

```csharp
DbContextConfiguration.Configure(options =>
{
#if UseSqlite
    options.SynchronousWriteOptions.Enable = true;// 启用同步写入模式：解决多线程并发写入导致的锁库问题
    options.SynchronousWriteOptions.LockTimeout = 30000;// 同步写入锁等待超时时间（单位：毫秒），默认值：10000
    options.SynchronousWriteOptions.OnLockTakenFailed = lockTimeout =>
    {
        Console.WriteLine($"######获取同步写入锁失败({lockTimeout}ms)");
        return true;// 返回true：继续执行（仍然可能会发生锁库问题）。返回false：不再继续执行，直接返回默认值
    };
#endif
});
```