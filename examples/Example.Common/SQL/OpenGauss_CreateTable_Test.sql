CREATE TABLE "{$TableName$}" (
  "Id" SERIAL,
  "UserId" bigint NOT NULL,
  "UserName" varchar(50),
  "Age" integer NOT NULL,
  "Sex" integer NOT NULL,
  "PhoneNumber" varchar(50),
  "Email" varchar(50),
  "IsVip" boolean NOT NULL,
  "IsBlack" boolean NOT NULL,
  "Country" integer NOT NULL,
  "AccountBalance" numeric(18,2) NOT NULL,
  "AccountBalance2" numeric(18,2) NOT NULL,
  "Status" integer NOT NULL,
  "Remark" varchar(255),
  "CreateTime" timestamp with time zone NOT NULL,
  "UpdateTime" timestamp with time zone,
  CONSTRAINT "PK_{$TableName$}" PRIMARY KEY ("Id")
);
COMMENT ON TABLE "{$TableName$}" IS '测试表';
COMMENT ON COLUMN "{$TableName$}"."Id" IS '自增主键';
COMMENT ON COLUMN "{$TableName$}"."UserId" IS '用户id';
COMMENT ON COLUMN "{$TableName$}"."UserName" IS '用户名称';
COMMENT ON COLUMN "{$TableName$}"."Age" IS '年龄';
COMMENT ON COLUMN "{$TableName$}"."Sex" IS '性别';
COMMENT ON COLUMN "{$TableName$}"."PhoneNumber" IS '电话号码';
COMMENT ON COLUMN "{$TableName$}"."Email" IS '邮箱';
COMMENT ON COLUMN "{$TableName$}"."IsVip" IS '是否VIP用户';
COMMENT ON COLUMN "{$TableName$}"."IsBlack" IS '是否黑名单用户';
COMMENT ON COLUMN "{$TableName$}"."Country" IS '国家';
COMMENT ON COLUMN "{$TableName$}"."AccountBalance" IS '账户余额';
COMMENT ON COLUMN "{$TableName$}"."AccountBalance2" IS '账户余额';
COMMENT ON COLUMN "{$TableName$}"."Status" IS '状态';
COMMENT ON COLUMN "{$TableName$}"."Remark" IS '备注';
COMMENT ON COLUMN "{$TableName$}"."CreateTime" IS '创建时间';
COMMENT ON COLUMN "{$TableName$}"."UpdateTime" IS '更新时间';

---- Create the first test table
--CREATE TABLE table1 (
--  id SERIAL PRIMARY KEY,
--  name VARCHAR(50)
--);
---- Create the second test table with a foreign key constraint referencing table1
--CREATE TABLE table2 (
--  id SERIAL PRIMARY KEY,
--  table1_id INT REFERENCES table1(id),
--  description TEXT
--);