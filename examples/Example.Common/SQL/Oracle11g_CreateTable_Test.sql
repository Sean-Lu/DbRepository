-- 在 Oracle 中设置自增字段，需要根据不同的版本使用不同的方法：
-- 1. 在 Oracle 11g 中，需要先创建序列（SQUENCE）再创建一个触发器（TRIGGER）。
-- 2. 在 Oracle 12c 中，只需要使用 IDENTITY 属性就可以了。


-- 1. 创建表
--CREATE TABLE
--"{$TableName$}" (
--    "Id" NUMBER(19) PRIMARY KEY,
--    "UserId" NUMBER(19) NOT NULL,
--    "UserName" NVARCHAR2(50),
--    "Age" NUMBER(10) NOT NULL,
--    "Sex" NUMBER(10) NOT NULL,
--    "PhoneNumber" NVARCHAR2(50),
--    "Email" NVARCHAR2(50),
--    "IsVip" NUMBER(1) NOT NULL,
--    "IsBlack" NUMBER(1) NOT NULL,
--    "Country" NUMBER(10) NOT NULL,
--    "AccountBalance" DECIMAL(18, 2) NOT NULL,
--    "AccountBalance2" DECIMAL(18, 2) NOT NULL,
--    "Status" NUMBER(10) NOT NULL,
--    "Remark" NVARCHAR2(255),
--    "CreateTime" TIMESTAMP(7) NOT NULL,
--    "UpdateTime" TIMESTAMP(7)
--);

-- 2. 创建序列
--CREATE SEQUENCE SQ_{$TableName$}
--INCREMENT BY 1
--START WITH 1
--NOMAXVALUE
--NOCYCLE
--NOCACHE;

-- 3. 创建触发器
--CREATE OR REPLACE TRIGGER TR_{$TableName$}
--BEFORE INSERT ON "{$TableName$}"
--FOR EACH ROW
--WHEN (NEW."Id" IS NULL)
--BEGIN
--  SELECT SQ_{$TableName$}.NEXTVAL
--  INTO :NEW."Id"
--  FROM DUAL;
--END;




BEGIN

execute immediate 'CREATE SEQUENCE "SQ_{$TableName$}" start with 1';

execute immediate 'CREATE TABLE "{$TableName$}" (
    "Id" NUMBER(19) NOT NULL,
    "UserId" NUMBER(19) NOT NULL,
    "UserName" NVARCHAR2(50),
    "Age" NUMBER(10) NOT NULL,
    "Sex" NUMBER(10) NOT NULL,
    "PhoneNumber" NVARCHAR2(50),
    "Email" NVARCHAR2(50),
    "IsVip" NUMBER(1) NOT NULL,
    "IsBlack" NUMBER(1) NOT NULL,
    "Country" NUMBER(10) NOT NULL,
    "AccountBalance" DECIMAL(18, 2) NOT NULL,
    "AccountBalance2" DECIMAL(18, 2) NOT NULL,
    "Status" NUMBER(10) NOT NULL,
    "Remark" NVARCHAR2(255),
    "CreateTime" TIMESTAMP(7) NOT NULL,
    "UpdateTime" TIMESTAMP(7),
    CONSTRAINT "PK_{$TableName$}" PRIMARY KEY ("Id")
)';

execute immediate 'create or replace trigger "TR_{$TableName$}"
before insert on "{$TableName$}" for each row
begin
  if :new."Id" is NULL then
    select "SQ_{$TableName$}".nextval into :new."Id" from dual;
  end if;
end;';

END;