## 简介

> `ORM`，支持所有**关系型数据库**（实现`DbProviderFactory`），如：`MySQL`、`SQL Server`、`Oracle`、`SQLite`、`Access`、`Firebird`、`PostgreSql`、`DB2`、`Informix`等

- 核心类：

| Class                                         | 说明                                   |
| --------------------------------------------- | -------------------------------------- |
| `BaseRepository<TEntity>`<br>`BaseRepository` | 抽象类，基于`DbFactory`+`Dapper`扩展   |
| `DbFactory`                                   | 数据库工厂                             |
| `DbProviderFactoryManager`                    | `System.Data.Common.DbProviderFactory` |

- `DbFactory`特别说明：

```
Get<T>()、GetList<T>() 其中 T ：
1. 支持自定义的Model实体模型（字段映射默认不区分大小写：IgnoreCaseWhenMatchField = true）
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
			<add name="MySQL Data Provider" invariant="MySql.Data.MySqlClient" description=".Net Framework Data Provider for MySQL" type="MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data, Version=8.0.25, Culture=neutral, PublicKeyToken=c5687fc88969c44d"/>
		</DbProviderFactories>
	</system.data>
</configuration>
```

- `.NET Core`: `appsettings.json`【通过`ProviderName`或`DatabaseType`（枚举）来配置数据库客户端驱动】

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

## 使用示例

- 项目：`examples\Example.NetCore`
- 项目：`examples\Example.NetFramework`

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