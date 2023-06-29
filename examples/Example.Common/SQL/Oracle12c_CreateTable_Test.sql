﻿-- 在 Oracle 中设置自增字段，需要根据不同的版本使用不同的方法：
-- 1. 在 Oracle 11g 中，需要先创建序列（SQUENCE）再创建一个触发器（TRIGGER）。
-- 2. 在 Oracle 12c 中，只需要使用 IDENTITY 属性就可以了。


CREATE TABLE "{$TableName$}" (
    "Id" NUMBER(19) GENERATED BY DEFAULT ON NULL AS IDENTITY NOT NULL,
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
);