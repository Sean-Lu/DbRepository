using System;
using Example.Dapper.Core.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.CodeFirst;

namespace Sean.Core.DbRepository.Test
{
    [TestClass]
    public class CodeFirstTest : TestBase
    {
        [TestMethod]
        public void TestCreatTableSqlForMySQL()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.MySql);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE `Test` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键',
  `UserId` bigint COMMENT '用户主键',
  `UserName` varchar(50) COMMENT '用户名称',
  `Age` int DEFAULT 18 COMMENT '年龄',
  `Sex` int COMMENT '性别',
  `PhoneNumber` varchar(50) COMMENT '电话号码',
  `Email` varchar(50) DEFAULT 'user@sample.com' COMMENT '邮箱',
  `IsVip` bit(1) DEFAULT 1 COMMENT '是否VIP用户',
  `IsBlack` bit(1) DEFAULT 0 COMMENT '是否黑名单用户',
  `Country` int DEFAULT 1 COMMENT '国家',
  `AccountBalance` decimal(18,2) DEFAULT 999.98 COMMENT '账户余额',
  `AccountBalance2` decimal(18,2) DEFAULT 9.98 COMMENT '账户余额',
  `Status` int DEFAULT 0 COMMENT '状态',
  `Remark` varchar(255) COMMENT '备注',
  `CreateTime` datetime COMMENT '创建时间',
  `UpdateTime` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) COMMENT='测试表';", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForMariaDB()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.MariaDB);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE `Test` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键',
  `UserId` bigint COMMENT '用户主键',
  `UserName` varchar(50) COMMENT '用户名称',
  `Age` int DEFAULT 18 COMMENT '年龄',
  `Sex` int COMMENT '性别',
  `PhoneNumber` varchar(50) COMMENT '电话号码',
  `Email` varchar(50) DEFAULT 'user@sample.com' COMMENT '邮箱',
  `IsVip` bit(1) DEFAULT 1 COMMENT '是否VIP用户',
  `IsBlack` bit(1) DEFAULT 0 COMMENT '是否黑名单用户',
  `Country` int DEFAULT 1 COMMENT '国家',
  `AccountBalance` decimal(18,2) DEFAULT 999.98 COMMENT '账户余额',
  `AccountBalance2` decimal(18,2) DEFAULT 9.98 COMMENT '账户余额',
  `Status` int DEFAULT 0 COMMENT '状态',
  `Remark` varchar(255) COMMENT '备注',
  `CreateTime` datetime COMMENT '创建时间',
  `UpdateTime` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) COMMENT='测试表';", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForTiDB()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.TiDB);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE `Test` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键',
  `UserId` bigint COMMENT '用户主键',
  `UserName` varchar(50) COMMENT '用户名称',
  `Age` int DEFAULT 18 COMMENT '年龄',
  `Sex` int COMMENT '性别',
  `PhoneNumber` varchar(50) COMMENT '电话号码',
  `Email` varchar(50) DEFAULT 'user@sample.com' COMMENT '邮箱',
  `IsVip` bit(1) DEFAULT 1 COMMENT '是否VIP用户',
  `IsBlack` bit(1) DEFAULT 0 COMMENT '是否黑名单用户',
  `Country` int DEFAULT 1 COMMENT '国家',
  `AccountBalance` decimal(18,2) DEFAULT 999.98 COMMENT '账户余额',
  `AccountBalance2` decimal(18,2) DEFAULT 9.98 COMMENT '账户余额',
  `Status` int DEFAULT 0 COMMENT '状态',
  `Remark` varchar(255) COMMENT '备注',
  `CreateTime` datetime COMMENT '创建时间',
  `UpdateTime` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) COMMENT='测试表';", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForOceanBase()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.OceanBase);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE `Test` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键',
  `UserId` bigint COMMENT '用户主键',
  `UserName` varchar(50) COMMENT '用户名称',
  `Age` int DEFAULT 18 COMMENT '年龄',
  `Sex` int COMMENT '性别',
  `PhoneNumber` varchar(50) COMMENT '电话号码',
  `Email` varchar(50) DEFAULT 'user@sample.com' COMMENT '邮箱',
  `IsVip` bit(1) DEFAULT 1 COMMENT '是否VIP用户',
  `IsBlack` bit(1) DEFAULT 0 COMMENT '是否黑名单用户',
  `Country` int DEFAULT 1 COMMENT '国家',
  `AccountBalance` decimal(18,2) DEFAULT 999.98 COMMENT '账户余额',
  `AccountBalance2` decimal(18,2) DEFAULT 9.98 COMMENT '账户余额',
  `Status` int DEFAULT 0 COMMENT '状态',
  `Remark` varchar(255) COMMENT '备注',
  `CreateTime` datetime COMMENT '创建时间',
  `UpdateTime` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) COMMENT='测试表';", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForSqlServer()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.SqlServer);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE [Test] (
  [Id] bigint NOT NULL IDENTITY,
  [UserId] bigint,
  [UserName] nvarchar(50),
  [Age] int DEFAULT 18,
  [Sex] int,
  [PhoneNumber] nvarchar(50),
  [Email] nvarchar(50) DEFAULT 'user@sample.com',
  [IsVip] bit DEFAULT 1,
  [IsBlack] bit DEFAULT 0,
  [Country] int DEFAULT 1,
  [AccountBalance] decimal(18,2) DEFAULT 999.98,
  [AccountBalance2] decimal(18,2) DEFAULT 9.98,
  [Status] int DEFAULT 0,
  [Remark] nvarchar(255),
  [CreateTime] datetime2,
  [UpdateTime] datetime2,
  PRIMARY KEY ([Id])
);
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'测试表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'Id';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户主键', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'UserId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户名称', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'UserName';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'年龄', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'Age';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'性别', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'Sex';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'电话号码', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'PhoneNumber';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'邮箱', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'Email';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否VIP用户', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'IsVip';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否黑名单用户', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'IsBlack';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'国家', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'Country';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'账户余额', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'AccountBalance';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'账户余额', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'AccountBalance2';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'状态', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'Status';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备注', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'Remark';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'CreateTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'更新时间', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Test', @level2type=N'COLUMN',@level2name=N'UpdateTime';
", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForOracle()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.Oracle);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"BEGIN
execute immediate 'CREATE TABLE ""Test"" (
  ""Id"" NUMBER(19) GENERATED BY DEFAULT ON NULL AS IDENTITY NOT NULL,
  ""UserId"" NUMBER(19),
  ""UserName"" NVARCHAR2(50),
  ""Age"" NUMBER(10) DEFAULT 18,
  ""Sex"" NUMBER(10),
  ""PhoneNumber"" NVARCHAR2(50),
  ""Email"" NVARCHAR2(50) DEFAULT ''user@sample.com'',
  ""IsVip"" NUMBER(1) DEFAULT 1,
  ""IsBlack"" NUMBER(1) DEFAULT 0,
  ""Country"" NUMBER(10) DEFAULT 1,
  ""AccountBalance"" DECIMAL(18,2) DEFAULT 999.98,
  ""AccountBalance2"" DECIMAL(18,2) DEFAULT 9.98,
  ""Status"" NUMBER(10) DEFAULT 0,
  ""Remark"" NVARCHAR2(255),
  ""CreateTime"" TIMESTAMP(7),
  ""UpdateTime"" TIMESTAMP(7),
  PRIMARY KEY (""Id"")
)';
execute immediate 'COMMENT ON TABLE ""Test"" IS ''测试表''';
execute immediate 'COMMENT ON COLUMN ""Test"".""Id"" IS ''主键''';
execute immediate 'COMMENT ON COLUMN ""Test"".""UserId"" IS ''用户主键''';
execute immediate 'COMMENT ON COLUMN ""Test"".""UserName"" IS ''用户名称''';
execute immediate 'COMMENT ON COLUMN ""Test"".""Age"" IS ''年龄''';
execute immediate 'COMMENT ON COLUMN ""Test"".""Sex"" IS ''性别''';
execute immediate 'COMMENT ON COLUMN ""Test"".""PhoneNumber"" IS ''电话号码''';
execute immediate 'COMMENT ON COLUMN ""Test"".""Email"" IS ''邮箱''';
execute immediate 'COMMENT ON COLUMN ""Test"".""IsVip"" IS ''是否VIP用户''';
execute immediate 'COMMENT ON COLUMN ""Test"".""IsBlack"" IS ''是否黑名单用户''';
execute immediate 'COMMENT ON COLUMN ""Test"".""Country"" IS ''国家''';
execute immediate 'COMMENT ON COLUMN ""Test"".""AccountBalance"" IS ''账户余额''';
execute immediate 'COMMENT ON COLUMN ""Test"".""AccountBalance2"" IS ''账户余额''';
execute immediate 'COMMENT ON COLUMN ""Test"".""Status"" IS ''状态''';
execute immediate 'COMMENT ON COLUMN ""Test"".""Remark"" IS ''备注''';
execute immediate 'COMMENT ON COLUMN ""Test"".""CreateTime"" IS ''创建时间''';
execute immediate 'COMMENT ON COLUMN ""Test"".""UpdateTime"" IS ''更新时间''';
END;", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForSQLite()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.SQLite);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE `Test` (
  `Id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  `UserId` INTEGER,
  `UserName` TEXT,
  `Age` INTEGER DEFAULT 18,
  `Sex` INTEGER,
  `PhoneNumber` TEXT,
  `Email` TEXT DEFAULT 'user@sample.com',
  `IsVip` INTEGER DEFAULT 1,
  `IsBlack` INTEGER DEFAULT 0,
  `Country` INTEGER DEFAULT 1,
  `AccountBalance` DECIMAL(18,2) DEFAULT 999.98,
  `AccountBalance2` DECIMAL(18,2) DEFAULT 9.98,
  `Status` INTEGER DEFAULT 0,
  `Remark` TEXT,
  `CreateTime` TEXT,
  `UpdateTime` TEXT
);", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForDuckDB()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.DuckDB);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE SEQUENCE ""SQ_Test"";
CREATE TABLE ""Test"" (
  ""Id"" BIGINT NOT NULL DEFAULT nextval('SQ_Test'),
  ""UserId"" BIGINT,
  ""UserName"" VARCHAR(50),
  ""Age"" INTEGER DEFAULT 18,
  ""Sex"" INTEGER,
  ""PhoneNumber"" VARCHAR(50),
  ""Email"" VARCHAR(50) DEFAULT 'user@sample.com',
  ""IsVip"" BOOLEAN DEFAULT 1,
  ""IsBlack"" BOOLEAN DEFAULT 0,
  ""Country"" INTEGER DEFAULT 1,
  ""AccountBalance"" DECIMAL(18,2) DEFAULT 999.98,
  ""AccountBalance2"" DECIMAL(18,2) DEFAULT 9.98,
  ""Status"" INTEGER DEFAULT 0,
  ""Remark"" VARCHAR(255),
  ""CreateTime"" TIMESTAMP,
  ""UpdateTime"" TIMESTAMP,
  PRIMARY KEY (""Id"")
);", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForMsAccess()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.MsAccess);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE [Test] (
  [Id] AUTOINCREMENT NOT NULL,
  [UserId] LONG,
  [UserName] VARCHAR(50),
  [Age] INTEGER DEFAULT 18,
  [Sex] INTEGER,
  [PhoneNumber] VARCHAR(50),
  [Email] VARCHAR(50) DEFAULT 'user@sample.com',
  [IsVip] BIT DEFAULT 1,
  [IsBlack] BIT DEFAULT 0,
  [Country] INTEGER DEFAULT 1,
  [AccountBalance] DECIMAL(18,2) DEFAULT 999.98,
  [AccountBalance2] DECIMAL(18,2) DEFAULT 9.98,
  [Status] INTEGER DEFAULT 0,
  [Remark] VARCHAR(255),
  [CreateTime] TIMESTAMP,
  [UpdateTime] TIMESTAMP,
  PRIMARY KEY ([Id])
);", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForFirebird()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.Firebird);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
  ""UserId"" BIGINT,
  ""UserName"" BLOB SUB_TYPE TEXT,
  ""Age"" INTEGER DEFAULT 18,
  ""Sex"" INTEGER,
  ""PhoneNumber"" BLOB SUB_TYPE TEXT,
  ""Email"" BLOB SUB_TYPE TEXT DEFAULT 'user@sample.com',
  ""IsVip"" BOOLEAN DEFAULT 1,
  ""IsBlack"" BOOLEAN DEFAULT 0,
  ""Country"" INTEGER DEFAULT 1,
  ""AccountBalance"" DECIMAL(18,2) DEFAULT 999.98,
  ""AccountBalance2"" DECIMAL(18,2) DEFAULT 9.98,
  ""Status"" INTEGER DEFAULT 0,
  ""Remark"" BLOB SUB_TYPE TEXT,
  ""CreateTime"" TIMESTAMP,
  ""UpdateTime"" TIMESTAMP,
  PRIMARY KEY (""Id"")
);
COMMENT ON TABLE ""Test"" IS '测试表';
COMMENT ON COLUMN ""Test"".""Id"" IS '主键';
COMMENT ON COLUMN ""Test"".""UserId"" IS '用户主键';
COMMENT ON COLUMN ""Test"".""UserName"" IS '用户名称';
COMMENT ON COLUMN ""Test"".""Age"" IS '年龄';
COMMENT ON COLUMN ""Test"".""Sex"" IS '性别';
COMMENT ON COLUMN ""Test"".""PhoneNumber"" IS '电话号码';
COMMENT ON COLUMN ""Test"".""Email"" IS '邮箱';
COMMENT ON COLUMN ""Test"".""IsVip"" IS '是否VIP用户';
COMMENT ON COLUMN ""Test"".""IsBlack"" IS '是否黑名单用户';
COMMENT ON COLUMN ""Test"".""Country"" IS '国家';
COMMENT ON COLUMN ""Test"".""AccountBalance"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""AccountBalance2"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""Status"" IS '状态';
COMMENT ON COLUMN ""Test"".""Remark"" IS '备注';
COMMENT ON COLUMN ""Test"".""CreateTime"" IS '创建时间';
COMMENT ON COLUMN ""Test"".""UpdateTime"" IS '更新时间';", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForPostgreSql()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.PostgreSql);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
  ""UserId"" bigint,
  ""UserName"" varchar(50),
  ""Age"" integer DEFAULT 18,
  ""Sex"" integer,
  ""PhoneNumber"" varchar(50),
  ""Email"" varchar(50) DEFAULT 'user@sample.com',
  ""IsVip"" boolean DEFAULT 1,
  ""IsBlack"" boolean DEFAULT 0,
  ""Country"" integer DEFAULT 1,
  ""AccountBalance"" numeric(18,2) DEFAULT 999.98,
  ""AccountBalance2"" numeric(18,2) DEFAULT 9.98,
  ""Status"" integer DEFAULT 0,
  ""Remark"" varchar(255),
  ""CreateTime"" timestamp with time zone,
  ""UpdateTime"" timestamp with time zone,
  PRIMARY KEY (""Id"")
);
COMMENT ON TABLE ""Test"" IS '测试表';
COMMENT ON COLUMN ""Test"".""Id"" IS '主键';
COMMENT ON COLUMN ""Test"".""UserId"" IS '用户主键';
COMMENT ON COLUMN ""Test"".""UserName"" IS '用户名称';
COMMENT ON COLUMN ""Test"".""Age"" IS '年龄';
COMMENT ON COLUMN ""Test"".""Sex"" IS '性别';
COMMENT ON COLUMN ""Test"".""PhoneNumber"" IS '电话号码';
COMMENT ON COLUMN ""Test"".""Email"" IS '邮箱';
COMMENT ON COLUMN ""Test"".""IsVip"" IS '是否VIP用户';
COMMENT ON COLUMN ""Test"".""IsBlack"" IS '是否黑名单用户';
COMMENT ON COLUMN ""Test"".""Country"" IS '国家';
COMMENT ON COLUMN ""Test"".""AccountBalance"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""AccountBalance2"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""Status"" IS '状态';
COMMENT ON COLUMN ""Test"".""Remark"" IS '备注';
COMMENT ON COLUMN ""Test"".""CreateTime"" IS '创建时间';
COMMENT ON COLUMN ""Test"".""UpdateTime"" IS '更新时间';
", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForOpenGauss()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.OpenGauss);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" SERIAL,
  ""UserId"" bigint,
  ""UserName"" varchar(50),
  ""Age"" integer DEFAULT 18,
  ""Sex"" integer,
  ""PhoneNumber"" varchar(50),
  ""Email"" varchar(50) DEFAULT 'user@sample.com',
  ""IsVip"" boolean DEFAULT 1,
  ""IsBlack"" boolean DEFAULT 0,
  ""Country"" integer DEFAULT 1,
  ""AccountBalance"" numeric(18,2) DEFAULT 999.98,
  ""AccountBalance2"" numeric(18,2) DEFAULT 9.98,
  ""Status"" integer DEFAULT 0,
  ""Remark"" varchar(255),
  ""CreateTime"" timestamp with time zone,
  ""UpdateTime"" timestamp with time zone,
  PRIMARY KEY (""Id"")
);
COMMENT ON TABLE ""Test"" IS '测试表';
COMMENT ON COLUMN ""Test"".""Id"" IS '主键';
COMMENT ON COLUMN ""Test"".""UserId"" IS '用户主键';
COMMENT ON COLUMN ""Test"".""UserName"" IS '用户名称';
COMMENT ON COLUMN ""Test"".""Age"" IS '年龄';
COMMENT ON COLUMN ""Test"".""Sex"" IS '性别';
COMMENT ON COLUMN ""Test"".""PhoneNumber"" IS '电话号码';
COMMENT ON COLUMN ""Test"".""Email"" IS '邮箱';
COMMENT ON COLUMN ""Test"".""IsVip"" IS '是否VIP用户';
COMMENT ON COLUMN ""Test"".""IsBlack"" IS '是否黑名单用户';
COMMENT ON COLUMN ""Test"".""Country"" IS '国家';
COMMENT ON COLUMN ""Test"".""AccountBalance"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""AccountBalance2"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""Status"" IS '状态';
COMMENT ON COLUMN ""Test"".""Remark"" IS '备注';
COMMENT ON COLUMN ""Test"".""CreateTime"" IS '创建时间';
COMMENT ON COLUMN ""Test"".""UpdateTime"" IS '更新时间';
", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForHighgoDB()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.HighgoDB);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
  ""UserId"" bigint,
  ""UserName"" varchar(50),
  ""Age"" integer DEFAULT 18,
  ""Sex"" integer,
  ""PhoneNumber"" varchar(50),
  ""Email"" varchar(50) DEFAULT 'user@sample.com',
  ""IsVip"" boolean DEFAULT 1,
  ""IsBlack"" boolean DEFAULT 0,
  ""Country"" integer DEFAULT 1,
  ""AccountBalance"" numeric(18,2) DEFAULT 999.98,
  ""AccountBalance2"" numeric(18,2) DEFAULT 9.98,
  ""Status"" integer DEFAULT 0,
  ""Remark"" varchar(255),
  ""CreateTime"" timestamp with time zone,
  ""UpdateTime"" timestamp with time zone,
  PRIMARY KEY (""Id"")
);
COMMENT ON TABLE ""Test"" IS '测试表';
COMMENT ON COLUMN ""Test"".""Id"" IS '主键';
COMMENT ON COLUMN ""Test"".""UserId"" IS '用户主键';
COMMENT ON COLUMN ""Test"".""UserName"" IS '用户名称';
COMMENT ON COLUMN ""Test"".""Age"" IS '年龄';
COMMENT ON COLUMN ""Test"".""Sex"" IS '性别';
COMMENT ON COLUMN ""Test"".""PhoneNumber"" IS '电话号码';
COMMENT ON COLUMN ""Test"".""Email"" IS '邮箱';
COMMENT ON COLUMN ""Test"".""IsVip"" IS '是否VIP用户';
COMMENT ON COLUMN ""Test"".""IsBlack"" IS '是否黑名单用户';
COMMENT ON COLUMN ""Test"".""Country"" IS '国家';
COMMENT ON COLUMN ""Test"".""AccountBalance"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""AccountBalance2"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""Status"" IS '状态';
COMMENT ON COLUMN ""Test"".""Remark"" IS '备注';
COMMENT ON COLUMN ""Test"".""CreateTime"" IS '创建时间';
COMMENT ON COLUMN ""Test"".""UpdateTime"" IS '更新时间';
", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForIvorySQL()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.IvorySQL);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
  ""UserId"" bigint,
  ""UserName"" varchar(50),
  ""Age"" integer DEFAULT 18,
  ""Sex"" integer,
  ""PhoneNumber"" varchar(50),
  ""Email"" varchar(50) DEFAULT 'user@sample.com',
  ""IsVip"" boolean DEFAULT 1,
  ""IsBlack"" boolean DEFAULT 0,
  ""Country"" integer DEFAULT 1,
  ""AccountBalance"" numeric(18,2) DEFAULT 999.98,
  ""AccountBalance2"" numeric(18,2) DEFAULT 9.98,
  ""Status"" integer DEFAULT 0,
  ""Remark"" varchar(255),
  ""CreateTime"" timestamp with time zone,
  ""UpdateTime"" timestamp with time zone,
  PRIMARY KEY (""Id"")
);
COMMENT ON TABLE ""Test"" IS '测试表';
COMMENT ON COLUMN ""Test"".""Id"" IS '主键';
COMMENT ON COLUMN ""Test"".""UserId"" IS '用户主键';
COMMENT ON COLUMN ""Test"".""UserName"" IS '用户名称';
COMMENT ON COLUMN ""Test"".""Age"" IS '年龄';
COMMENT ON COLUMN ""Test"".""Sex"" IS '性别';
COMMENT ON COLUMN ""Test"".""PhoneNumber"" IS '电话号码';
COMMENT ON COLUMN ""Test"".""Email"" IS '邮箱';
COMMENT ON COLUMN ""Test"".""IsVip"" IS '是否VIP用户';
COMMENT ON COLUMN ""Test"".""IsBlack"" IS '是否黑名单用户';
COMMENT ON COLUMN ""Test"".""Country"" IS '国家';
COMMENT ON COLUMN ""Test"".""AccountBalance"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""AccountBalance2"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""Status"" IS '状态';
COMMENT ON COLUMN ""Test"".""Remark"" IS '备注';
COMMENT ON COLUMN ""Test"".""CreateTime"" IS '创建时间';
COMMENT ON COLUMN ""Test"".""UpdateTime"" IS '更新时间';
", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForQuestDB()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.QuestDB);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" LONG NOT NULL,
  ""UserId"" LONG,
  ""UserName"" STRING,
  ""Age"" INT DEFAULT 18,
  ""Sex"" INT,
  ""PhoneNumber"" STRING,
  ""Email"" STRING DEFAULT 'user@sample.com',
  ""IsVip"" BOOLEAN DEFAULT TRUE,
  ""IsBlack"" BOOLEAN DEFAULT FALSE,
  ""Country"" INT DEFAULT 1,
  ""AccountBalance"" DOUBLE DEFAULT 999.98,
  ""AccountBalance2"" DOUBLE DEFAULT 9.98,
  ""Status"" INT DEFAULT 0,
  ""Remark"" STRING,
  ""CreateTime"" TIMESTAMP,
  ""UpdateTime"" TIMESTAMP
);", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForDB2()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.DB2);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" BIGINT NOT NULL GENERATED ALWAYS AS IDENTITY (START WITH 1, INCREMENT BY 1),
  ""UserId"" BIGINT,
  ""UserName"" VARCHAR(50),
  ""Age"" INTEGER DEFAULT 18,
  ""Sex"" INTEGER,
  ""PhoneNumber"" VARCHAR(50),
  ""Email"" VARCHAR(50) DEFAULT 'user@sample.com',
  ""IsVip"" INTEGER DEFAULT 1,
  ""IsBlack"" INTEGER DEFAULT 0,
  ""Country"" INTEGER DEFAULT 1,
  ""AccountBalance"" DECIMAL(18,2) DEFAULT 999.98,
  ""AccountBalance2"" DECIMAL(18,2) DEFAULT 9.98,
  ""Status"" INTEGER DEFAULT 0,
  ""Remark"" VARCHAR(255),
  ""CreateTime"" TIMESTAMP,
  ""UpdateTime"" TIMESTAMP,
  PRIMARY KEY (""Id"")
);", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForInformix()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.Informix);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" SERIAL NOT NULL,
  ""UserId"" BIGINT,
  ""UserName"" VARCHAR(50),
  ""Age"" INTEGER DEFAULT 18,
  ""Sex"" INTEGER,
  ""PhoneNumber"" VARCHAR(50),
  ""Email"" VARCHAR(50) DEFAULT 'user@sample.com',
  ""IsVip"" BOOLEAN DEFAULT 't',
  ""IsBlack"" BOOLEAN DEFAULT 'f',
  ""Country"" INTEGER DEFAULT 1,
  ""AccountBalance"" DECIMAL(18,2) DEFAULT 999.98,
  ""AccountBalance2"" DECIMAL(18,2) DEFAULT 9.98,
  ""Status"" INTEGER DEFAULT 0,
  ""Remark"" VARCHAR(255),
  ""CreateTime"" DATETIME YEAR TO FRACTION(5),
  ""UpdateTime"" DATETIME YEAR TO FRACTION(5),
  PRIMARY KEY (""Id"")
);", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForClickHouse()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.ClickHouse);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE `Test` (
  `Id` Int64 NOT NULL COMMENT '主键',
  `UserId` Nullable(Int64) COMMENT '用户主键',
  `UserName` Nullable(String) COMMENT '用户名称',
  `Age` Nullable(Int32) DEFAULT 18 COMMENT '年龄',
  `Sex` Nullable(Int32) COMMENT '性别',
  `PhoneNumber` Nullable(String) COMMENT '电话号码',
  `Email` Nullable(String) DEFAULT 'user@sample.com' COMMENT '邮箱',
  `IsVip` Nullable(Bool) DEFAULT 1 COMMENT '是否VIP用户',
  `IsBlack` Nullable(Bool) DEFAULT 0 COMMENT '是否黑名单用户',
  `Country` Nullable(Int32) DEFAULT 1 COMMENT '国家',
  `AccountBalance` Nullable(Decimal(18,2)) DEFAULT 999.98 COMMENT '账户余额',
  `AccountBalance2` Nullable(Decimal(18,2)) DEFAULT 9.98 COMMENT '账户余额',
  `Status` Nullable(Int32) DEFAULT 0 COMMENT '状态',
  `Remark` Nullable(String) COMMENT '备注',
  `CreateTime` Nullable(DateTime) COMMENT '创建时间',
  `UpdateTime` Nullable(DateTime) COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) ENGINE = MergeTree() COMMENT '测试表';", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForDameng()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.Dameng);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" BIGINT NOT NULL AUTO_INCREMENT COMMENT '主键',
  ""UserId"" BIGINT COMMENT '用户主键',
  ""UserName"" VARCHAR(50) COMMENT '用户名称',
  ""Age"" INT DEFAULT 18 COMMENT '年龄',
  ""Sex"" INT COMMENT '性别',
  ""PhoneNumber"" VARCHAR(50) COMMENT '电话号码',
  ""Email"" VARCHAR(50) DEFAULT 'user@sample.com' COMMENT '邮箱',
  ""IsVip"" BIT DEFAULT 1 COMMENT '是否VIP用户',
  ""IsBlack"" BIT DEFAULT 0 COMMENT '是否黑名单用户',
  ""Country"" INT DEFAULT 1 COMMENT '国家',
  ""AccountBalance"" DECIMAL(18,2) DEFAULT 999.98 COMMENT '账户余额',
  ""AccountBalance2"" DECIMAL(18,2) DEFAULT 9.98 COMMENT '账户余额',
  ""Status"" INT DEFAULT 0 COMMENT '状态',
  ""Remark"" VARCHAR(255) COMMENT '备注',
  ""CreateTime"" DATETIME COMMENT '创建时间',
  ""UpdateTime"" DATETIME COMMENT '更新时间',
  PRIMARY KEY (""Id"")
) COMMENT '测试表';", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForKingbaseES()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.KingbaseES);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
  ""UserId"" bigint,
  ""UserName"" varchar(50),
  ""Age"" integer DEFAULT 18,
  ""Sex"" integer,
  ""PhoneNumber"" varchar(50),
  ""Email"" varchar(50) DEFAULT 'user@sample.com',
  ""IsVip"" boolean DEFAULT 1,
  ""IsBlack"" boolean DEFAULT 0,
  ""Country"" integer DEFAULT 1,
  ""AccountBalance"" numeric(18,2) DEFAULT 999.98,
  ""AccountBalance2"" numeric(18,2) DEFAULT 9.98,
  ""Status"" integer DEFAULT 0,
  ""Remark"" varchar(255),
  ""CreateTime"" timestamp with time zone,
  ""UpdateTime"" timestamp with time zone,
  PRIMARY KEY (""Id"")
);
COMMENT ON TABLE ""Test"" IS '测试表';
COMMENT ON COLUMN ""Test"".""Id"" IS '主键';
COMMENT ON COLUMN ""Test"".""UserId"" IS '用户主键';
COMMENT ON COLUMN ""Test"".""UserName"" IS '用户名称';
COMMENT ON COLUMN ""Test"".""Age"" IS '年龄';
COMMENT ON COLUMN ""Test"".""Sex"" IS '性别';
COMMENT ON COLUMN ""Test"".""PhoneNumber"" IS '电话号码';
COMMENT ON COLUMN ""Test"".""Email"" IS '邮箱';
COMMENT ON COLUMN ""Test"".""IsVip"" IS '是否VIP用户';
COMMENT ON COLUMN ""Test"".""IsBlack"" IS '是否黑名单用户';
COMMENT ON COLUMN ""Test"".""Country"" IS '国家';
COMMENT ON COLUMN ""Test"".""AccountBalance"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""AccountBalance2"" IS '账户余额';
COMMENT ON COLUMN ""Test"".""Status"" IS '状态';
COMMENT ON COLUMN ""Test"".""Remark"" IS '备注';
COMMENT ON COLUMN ""Test"".""CreateTime"" IS '创建时间';
COMMENT ON COLUMN ""Test"".""UpdateTime"" IS '更新时间';
", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForShenTong()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.ShenTong);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" INT8 AUTO_INCREMENT NOT NULL,
  ""UserId"" INT8,
  ""UserName"" CLOB,
  ""Age"" INT4 DEFAULT 18,
  ""Sex"" INT4,
  ""PhoneNumber"" CLOB,
  ""Email"" CLOB DEFAULT 'user@sample.com',
  ""IsVip"" BOOL DEFAULT 1,
  ""IsBlack"" BOOL DEFAULT 0,
  ""Country"" INT4 DEFAULT 1,
  ""AccountBalance"" DECIMAL(18,2) DEFAULT 999.98,
  ""AccountBalance2"" DECIMAL(18,2) DEFAULT 9.98,
  ""Status"" INT4 DEFAULT 0,
  ""Remark"" CLOB,
  ""CreateTime"" TIMESTAMP,
  ""UpdateTime"" TIMESTAMP,
  PRIMARY KEY (""Id"")
);", string.Join(Environment.NewLine, sql));
        }

        [TestMethod]
        public void TestCreatTableSqlForXugu()
        {
            ISqlGenerator sqlGenerator = SqlGeneratorFactory.GetSqlGenerator(DatabaseType.Xugu);
            var sql = sqlGenerator.GetCreateTableSql<TestEntity>();
            Assert.AreEqual(@"CREATE TABLE ""Test"" (
  ""Id"" bigint IDENTITY(1,1) COMMENT '主键',
  ""UserId"" bigint COMMENT '用户主键',
  ""UserName"" varchar(50) COMMENT '用户名称',
  ""Age"" int DEFAULT 18 COMMENT '年龄',
  ""Sex"" int COMMENT '性别',
  ""PhoneNumber"" varchar(50) COMMENT '电话号码',
  ""Email"" varchar(50) DEFAULT 'user@sample.com' COMMENT '邮箱',
  ""IsVip"" boolean DEFAULT true COMMENT '是否VIP用户',
  ""IsBlack"" boolean DEFAULT false COMMENT '是否黑名单用户',
  ""Country"" int DEFAULT 1 COMMENT '国家',
  ""AccountBalance"" decimal(18,2) DEFAULT 999.98 COMMENT '账户余额',
  ""AccountBalance2"" decimal(18,2) DEFAULT 9.98 COMMENT '账户余额',
  ""Status"" int DEFAULT 0 COMMENT '状态',
  ""Remark"" varchar(255) COMMENT '备注',
  ""CreateTime"" timestamp COMMENT '创建时间',
  ""UpdateTime"" timestamp COMMENT '更新时间',
  PRIMARY KEY (""Id"")
) COMMENT '测试表';", string.Join(Environment.NewLine, sql));
        }
    }
}
