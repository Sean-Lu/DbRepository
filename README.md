## 🌈 简介

> `ORM`框架，支持数据库：`MySQL`、`MariaDB`、`TiDB`、`SQL Server`、`Oracle`、`SQLite`、`MS Access`、`Firebird`、`PostgreSql`、`DB2`、`Informix`、`ClickHouse`、`DM（达梦）`、`KingbaseES（人大金仓）`

- 支持主从库分离（主库：增\删\改，从库：查）
- 支持分表（自定义表名规则）
- 支持`Expression`表达式树（自动转换为参数化SQL语句）
- 常用类：

| Class                           | Namespace                       | Description                    |
| ------------------------------- | ------------------------------- | ------------------------------ |
| `DbFactory`                     | `Sean.Core.DbRepository`        | 数据库工厂                     |
| `SqlFactory`                    | `Sean.Core.DbRepository`        | `SQL`创建工厂（CRUD）          |
| `BaseRepository`                | `Sean.Core.DbRepository`        | 基于`DbFactory`实现            |
| `EntityBaseRepository<TEntity>` | `Sean.Core.DbRepository`        | 基于`DbFactory`实现            |
| `BaseRepository<TEntity>`       | `Sean.Core.DbRepository.Dapper` | 基于`DbFactory` + `Dapper`实现 |

- `DbFactory`类：支持所有实现`DbProviderFactory`的数据库

```
Get<T>()、GetList<T>() 其中 T ：
1. 支持自定义的 Model 实体类
2. 支持 dynamic 动态类型
3. 支持值类型，如：int、long、double、decimal、DateTime、bool等
4. 支持 string 类型
```

## 💖 Nuget Packages

| Package                                                                                        | NuGet Stable                                                                                                                                                        | NuGet Pre-release                                                                                                                                                      | Downloads                                                                                                                                                            |
| ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Sean.Core.DbRepository](https://www.nuget.org/packages/Sean.Core.DbRepository/)               | [![Sean.Core.DbRepository](https://img.shields.io/nuget/v/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      | [![Sean.Core.DbRepository](https://img.shields.io/nuget/vpre/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      | [![Sean.Core.DbRepository](https://img.shields.io/nuget/dt/Sean.Core.DbRepository.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository/)                      |
| [Sean.Core.DbRepository.Dapper](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/v/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/vpre/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) | [![Sean.Core.DbRepository.Dapper](https://img.shields.io/nuget/dt/Sean.Core.DbRepository.Dapper.svg)](https://www.nuget.org/packages/Sean.Core.DbRepository.Dapper/) |

## 🍉 CRUD Test

> `TestRepository`

| Database                 | Test result |
| ------------------------ | ----------- |
| `MySQL`                  | ✅         |
| `MariaDB`                | ✅         |
| `TiDB`                   | ✅         |
| `SQL Server`             | ✅         |
| `Oracle`                 | ✅         |
| `SQLite`                 | ✅         |
| `MS Access`              | ✅         |
| `Firebird`               | ✅         |
| `PostgreSql`             | ✅         |
| `DB2`                    | ✅         |
| `Informix`               | ✅         |
| `ClickHouse`             | ✅         |
| `DM（达梦）`             | ✅         |
| `KingbaseES（人大金仓）` | ✅         |

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
        <!-- Master database: If multiple databases are configured, the suffix of the database name is a number starting with 1. -->
        <add name="master" connectionString="DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a" providerName="MySql.Data.MySqlClient"/>
        <!-- 从库：如果配置了多个数据库，数据库名称后缀是以1开始的数字。 -->
        <!-- Slave database: If multiple databases are configured, the suffix of the database name is a number starting with 1. -->
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

- 配置示例：**通过在数据库连接字符串中设置`ProviderName`或`DatabaseType`的值来指定数据库类型**

```json
{
  "ConnectionStrings": {
    // 主库：如果配置了多个数据库，数据库名称后缀是以1开始的数字。
    // Master database: If multiple databases are configured, the suffix of the database name is a number starting with 1.
    "master": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",
    // 从库：如果配置了多个数据库，数据库名称后缀是以1开始的数字。
    // Slave database: If multiple databases are configured, the suffix of the database name is a number starting with 1.
    "secondary1": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",
    "secondary2": "DataSource=127.0.0.1;Database=test;uid=root;pwd=12345!a;ProviderName=MySql.Data.MySqlClient",

    "test_SqlServer": "server=127.0.0.1;database=test;uid=sa;pwd=123456!a;DatabaseType=SqlServer",
    "test_Oracle": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XXX)));User ID=XXX;Password=XXX;Persist Security Info=True;DatabaseType=Oracle",
    "test_SQLite": "data source=.\\test.db;version=3;DatabaseType=SQLite"
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
var _db = new DbFactory("Database connection string...", MySqlClientFactory.Instance);
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
      <database name="SqlServer" providerInvariantName="System.Data.SqlClient" factoryTypeAssemblyQualifiedName="System.Data.SqlClient.SqlClientFactory,System.Data"/>
      <database name="Oracle" providerInvariantName="Oracle.ManagedDataAccess.Client" factoryTypeAssemblyQualifiedName="Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess"/>
      <database name="SQLite" providerInvariantName="System.Data.SQLite" factoryTypeAssemblyQualifiedName="System.Data.SQLite.SQLiteFactory,System.Data.SQLite"/>
      <database name="MsAccess" providerInvariantName="System.Data.OleDb" factoryTypeAssemblyQualifiedName="System.Data.OleDb.OleDbFactory,System.Data"/>
      <!--<database name="MsAccess" providerInvariantName="System.Data.Odbc" factoryTypeAssemblyQualifiedName="System.Data.Odbc.OdbcFactory,System.Data"/>-->
      <database name="Firebird" providerInvariantName="FirebirdSql.Data.FirebirdClient" factoryTypeAssemblyQualifiedName="FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,FirebirdSql.Data.FirebirdClient"/>
      <database name="PostgreSql" providerInvariantName="Npgsql" factoryTypeAssemblyQualifiedName="Npgsql.NpgsqlFactory,Npgsql"/>
      <database name="DB2" providerInvariantName="IBM.Data.DB2" factoryTypeAssemblyQualifiedName="IBM.Data.DB2.Core.DB2Factory,IBM.Data.DB2.Core"/>
      <database name="Informix" providerInvariantName="IBM.Data.Informix" factoryTypeAssemblyQualifiedName="IBM.Data.Informix.IfxFactory,IBM.Data.Informix"/>
      <database name="ClickHouse" providerInvariantName="ClickHouse.Client" factoryTypeAssemblyQualifiedName="ClickHouse.Client.ADO.ClickHouseConnectionFactory,ClickHouse.Client"/>
      <database name="DM" providerInvariantName="DM" factoryTypeAssemblyQualifiedName="Dm.DmClientFactory, DmProvider"/>
      <database name="KingbaseES" providerInvariantName="Kdbndp" factoryTypeAssemblyQualifiedName="Kdbndp.KdbndpFactory, Kdbndp"/>
    </databases>
  </dbProviderMap>
</configuration>
```

### 增删改查（CRUD）

> `IBaseRepository<TEntity>`

```csharp
// 新增数据：
_testRepository.Add(entity);

// 批量新增数据：
_testRepository.Add(entities);

// 新增或更新数据：
_testRepository.AddOrUpdate(entity);

// 批量新增或更新数据：
_testRepository.AddOrUpdate(entities);

// 删除数据：过滤条件默认为实体的主键字段
_testRepository.Delete(entity);

// 删除数据：自定义过滤条件
_testRepository.Delete(entity => entity.UserId == 10001 && entity.Status != 0);

// 删除全部数据：
_testRepository.Delete(entity => true);

// 删除全部数据：
_testRepository.DeleteAll();

// 更新数据：更新全部字段，过滤条件默认为实体的主键字段
_testRepository.Update(entity);

// 更新数据：更新部分字段，过滤条件默认为实体的主键字段
_testRepository.Update(entity, fieldExpression: entity => new { entity.Status, entity.UpdateTime });

// 更新数据：更新全部字段，自定义过滤条件
_testRepository.Update(entity, whereExpression: entity => entity.UserId == 10001 && entity.Status != 0);

// 更新数据：更新部分字段，自定义过滤条件
_testRepository.Update(entity, fieldExpression: entity => new { entity.Status, entity.UpdateTime }, whereExpression: entity => entity.UserId == 10001 && entity.Status != 0);

// 批量更新数据：更新全部字段，过滤条件默认为实体的主键字段
_testRepository.Update(entities);

// 批量更新数据：更新部分字段，过滤条件默认为实体的主键字段
_testRepository.Update(entities, fieldExpression: entity => new { entity.Status, entity.UpdateTime });

// 数值字段递增：
_testRepository.Increment(10.0M, fieldExpression: entity => entity.AccountBalance, whereExpression: entity => entity.Id == 10001);

// 数值字段递减：
_testRepository.Decrement(10.0M, fieldExpression: entity => entity.AccountBalance, whereExpression: entity => entity.Id == 10001);

// 查询数据：分页 + 排序
int pageIndex = 1;// 当前页号（最小值为1）
int pageSize = 10;// 页大小
OrderByCondition orderBy = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.CreateTime);
orderBy.Next = OrderByConditionBuilder<TestEntity>.Build(OrderByType.Asc, entity => entity.Id);
List<TestEntity> queryResult = _testRepository.Query(entity => entity.UserId == 10001, orderBy, pageIndex, pageSize)?.ToList();

// 查询单个数据：
TestEntity getResult = _testRepository.Get(entity => entity.Id == 2);

// 统计数量：
int countResult = _testRepository.Count(entity => entity.UserId == 10001);

// 数据是否存在：
bool exists = _testRepository.Exists(entity => entity.UserId == 10001);

// 更多使用示例在单元测试中：Sean.Core.DbRepository.Test.TableRepositoryTest
```

> 表达式树：**`Expression<Func<TEntity, bool>> whereExpression`**

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

> 表达式树：**`Expression<Func<TEntity, object>> fieldExpression`**

```csharp
// 单个字段：
entity => entity.Status

// 多个字段（匿名类型）：
entity => new { entity.Status, entity.UpdateTime }

// 更多使用示例在单元测试中：Sean.Core.DbRepository.Test.FieldExpressionTest
```

> 常用实体类注解：`TableEntity`

| Attribute                    | AttributeUsage | Namespace                                      | Description                                                |
| ---------------------------- | -------------- | ---------------------------------------------- | ---------------------------------------------------------- |
| `TableAttribute`             | Class          | `System.ComponentModel.DataAnnotations.Schema` | 自定义表名                                                 |
| `SequenceAttribute`          | Class          | `Sean.Core.DbRepository`                       | 指定序列号名称（生成自增Id）                               |
| `KeyAttribute`               | Property       | `System.ComponentModel.DataAnnotations`        | 标记为主键字段                                             |
| `DatabaseGeneratedAttribute` | Property       | `System.ComponentModel.DataAnnotations.Schema` | 设置数据库生成字段值的方式（通常和`KeyAttribute`一起使用） |
| `ColumnAttribute`            | Property       | `System.ComponentModel.DataAnnotations.Schema` | 自定义字段名                                               |
| `NotMappedAttribute`         | Property       | `System.ComponentModel.DataAnnotations.Schema` | 标记为为忽略字段                                           |
| ~~`ForeignKeyAttribute`~~    | Property       | `System.ComponentModel.DataAnnotations.Schema` | 标记为外键字段（***暂不支持***）                           |

## ❓ 常见问题

> `OleDb`和`ODBC`的区别？

1. `OleDb`是Microsoft开发的一种数据库连接技术，它是面向对象的，可以连接多种类型的数据库，包括`Access`、`Excel`、`SQL Server`等等。OleDb使用COM接口连接数据库，因此只能在Windows平台上使用。
2. `ODBC`是一种通用的数据库连接技术，它可以连接多种类型的数据库，包括`Access`、`Excel`、`SQL Server`等等。ODBC使用标准的API连接数据库，因此可以在多个平台上使用，包括Windows、Linux、Unix等等。

> 报错：System.ArgumentException:“The specified invariant name 'MySql.Data.MySqlClient' wasn't found in the list of registered .NET Data Providers.”

```
注：从`.NET Standard` 2.1版本开始（`.NET Core` >= 3.0）才有`System.Data.Common.DbProviderFactories`类

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
