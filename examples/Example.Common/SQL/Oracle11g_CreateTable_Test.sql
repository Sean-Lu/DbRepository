-- 在 Oracle 中设置自增字段，需要根据不同的版本使用不同的方法：
-- 1. 在 Oracle 11g 中，需要先创建序列（SQUENCE）再创建一个触发器（TRIGGER）。
-- 2. 在 Oracle 12c 中，只需要使用 IDENTITY 属性就可以了。


BEGIN
-- 1. 创建序列
execute immediate 'CREATE SEQUENCE "SQ_{$TableName$}" INCREMENT BY 1 START WITH 1 NOMAXVALUE NOCYCLE NOCACHE';
-- 2. 创建表
execute immediate 'CREATE TABLE "{$TableName$}" (
  "Id" NUMBER(19) NOT NULL,
  "UserId" NUMBER(19) NOT NULL,
  "UserName" NVARCHAR2(50),
  "Age" NUMBER(10) NOT NULL DEFAULT 18,
  "Sex" NUMBER(10) NOT NULL,
  "PhoneNumber" NVARCHAR2(50),
  "Email" NVARCHAR2(50) DEFAULT ''user@sample.com'',
  "IsVip" NUMBER(1) NOT NULL DEFAULT 1,
  "IsBlack" NUMBER(1) NOT NULL DEFAULT 0,
  "Country" NUMBER(10) NOT NULL DEFAULT 1,
  "AccountBalance" DECIMAL(18,2) NOT NULL DEFAULT 999.98,
  "AccountBalance2" DECIMAL(18,2) NOT NULL DEFAULT 9.98,
  "Status" NUMBER(10) NOT NULL DEFAULT 0,
  "Remark" NVARCHAR2(255),
  "CreateTime" TIMESTAMP(7) NOT NULL,
  "UpdateTime" TIMESTAMP(7),
  CONSTRAINT "PK_{$TableName$}" PRIMARY KEY ("Id")
)';
execute immediate 'COMMENT ON TABLE "{$TableName$}" IS ''测试表''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."Id" IS ''自增主键''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."UserId" IS ''用户id''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."UserName" IS ''用户名称''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."Age" IS ''年龄''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."Sex" IS ''性别''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."PhoneNumber" IS ''电话号码''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."Email" IS ''邮箱''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."IsVip" IS ''是否VIP用户''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."IsBlack" IS ''是否黑名单用户''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."Country" IS ''国家''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."AccountBalance" IS ''账户余额''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."AccountBalance2" IS ''账户余额''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."Status" IS ''状态''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."Remark" IS ''备注''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."CreateTime" IS ''创建时间''';
execute immediate 'COMMENT ON COLUMN "{$TableName$}"."UpdateTime" IS ''更新时间''';
-- 3. 创建触发器
execute immediate 'CREATE OR REPLACE TRIGGER "TR_{$TableName$}"
BEFORE INSERT ON "{$TableName$}" FOR EACH ROW
WHEN (NEW."Id" IS NULL)
BEGIN
  SELECT "SQ_{$TableName$}".NEXTVAL INTO :NEW."Id" FROM DUAL;
END;';
END;