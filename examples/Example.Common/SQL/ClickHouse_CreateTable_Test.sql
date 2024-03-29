﻿-- Generated by CodeFirst.ISqlGenerator.
CREATE TABLE `{$TableName$}` (
  `Id` Int64 NOT NULL COMMENT '自增主键',
  `UserId` Int64 NOT NULL COMMENT '用户id',
  `UserName` Nullable(String) COMMENT '用户名称',
  `Age` Int32 NOT NULL DEFAULT 18 COMMENT '年龄',
  `Sex` Int32 NOT NULL COMMENT '性别',
  `PhoneNumber` Nullable(String) COMMENT '电话号码',
  `Email` Nullable(String) DEFAULT 'user@sample.com' COMMENT '邮箱',
  `IsVip` Bool NOT NULL DEFAULT 1 COMMENT '是否VIP用户',
  `IsBlack` Bool NOT NULL DEFAULT 0 COMMENT '是否黑名单用户',
  `Country` Int32 NOT NULL DEFAULT 1 COMMENT '国家',
  `AccountBalance` Decimal(18,2) NOT NULL DEFAULT 999.98 COMMENT '账户余额',
  `AccountBalance2` Decimal(18,2) NOT NULL DEFAULT 9.98 COMMENT '账户余额',
  `Status` Int32 NOT NULL DEFAULT 0 COMMENT '状态',
  `Remark` Nullable(String) COMMENT '备注',
  `CreateTime` DateTime NOT NULL COMMENT '创建时间',
  `UpdateTime` Nullable(DateTime) DEFAULT now() COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) ENGINE = MergeTree() COMMENT '测试表';