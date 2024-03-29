﻿-- Generated by CodeFirst.ISqlGenerator.
CREATE TABLE "{$TableName$}" (
  "Id" LONG NOT NULL,
  "UserId" LONG NOT NULL,
  "UserName" STRING,
  "Age" INT NOT NULL DEFAULT 18,
  "Sex" INT NOT NULL,
  "PhoneNumber" STRING,
  "Email" STRING DEFAULT 'user@sample.com',
  "IsVip" BOOLEAN NOT NULL DEFAULT TRUE,
  "IsBlack" BOOLEAN NOT NULL DEFAULT FALSE,
  "Country" INT NOT NULL DEFAULT 1,
  "AccountBalance" DOUBLE NOT NULL DEFAULT 999.98,
  "AccountBalance2" DOUBLE NOT NULL DEFAULT 9.98,
  "Status" INT NOT NULL DEFAULT 0,
  "Remark" STRING,
  "CreateTime" TIMESTAMP NOT NULL,
  "UpdateTime" TIMESTAMP
);