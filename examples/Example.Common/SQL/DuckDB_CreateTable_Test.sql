﻿-- Generated by CodeFirst.ISqlGenerator.
CREATE SEQUENCE "SQ_{$TableName$}";
-- ### MultiSqlSeparator ###
CREATE TABLE "{$TableName$}" (
  "Id" BIGINT NOT NULL DEFAULT nextval('SQ_{$TableName$}'),
  "UserId" BIGINT NOT NULL,
  "UserName" VARCHAR(50),
  "Age" INTEGER NOT NULL DEFAULT 18,
  "Sex" INTEGER NOT NULL,
  "PhoneNumber" VARCHAR(50),
  "Email" VARCHAR(50) DEFAULT 'user@sample.com',
  "IsVip" BOOLEAN NOT NULL DEFAULT 1,
  "IsBlack" BOOLEAN NOT NULL DEFAULT 0,
  "Country" INTEGER NOT NULL DEFAULT 1,
  "AccountBalance" DECIMAL(18,2) NOT NULL DEFAULT 999.98,
  "AccountBalance2" DECIMAL(18,2) NOT NULL DEFAULT 9.98,
  "Status" INTEGER NOT NULL DEFAULT 0,
  "Remark" VARCHAR(255),
  "CreateTime" TIMESTAMP NOT NULL,
  "UpdateTime" TIMESTAMP,
  PRIMARY KEY ("Id")
);