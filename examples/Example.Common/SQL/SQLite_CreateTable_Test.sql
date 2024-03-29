﻿-- Generated by CodeFirst.ISqlGenerator.
CREATE TABLE `{$TableName$}` (
  `Id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  `UserId` INTEGER NOT NULL,
  `UserName` TEXT,
  `Age` INTEGER NOT NULL DEFAULT 18,
  `Sex` INTEGER NOT NULL,
  `PhoneNumber` TEXT,
  `Email` TEXT DEFAULT 'user@sample.com',
  `IsVip` INTEGER NOT NULL DEFAULT 1,
  `IsBlack` INTEGER NOT NULL DEFAULT 0,
  `Country` INTEGER NOT NULL DEFAULT 1,
  `AccountBalance` DECIMAL(18,2) NOT NULL DEFAULT 999.98,
  `AccountBalance2` DECIMAL(18,2) NOT NULL DEFAULT 9.98,
  `Status` INTEGER NOT NULL DEFAULT 0,
  `Remark` TEXT,
  `CreateTime` TEXT NOT NULL,
  `UpdateTime` TEXT
);