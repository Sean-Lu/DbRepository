﻿CREATE TABLE "{$TableName$}" (
    "Id" BIGINT NOT NULL AUTO_INCREMENT COMMENT '自增主键',
    "UserId" BIGINT NOT NULL COMMENT '用户id',
    "UserName" VARCHAR(50) DEFAULT NULL COMMENT '用户名称',
    "Age" INT NOT NULL DEFAULT '0' COMMENT '年龄',
    "Sex" SMALLINT NOT NULL DEFAULT '0' COMMENT '性别',
    "PhoneNumber" VARCHAR(50) DEFAULT NULL COMMENT '电话号码',
    "Email" VARCHAR(50) DEFAULT NULL COMMENT '邮箱',
    "IsVip" BIT DEFAULT '0' NOT NULL COMMENT '是否是VIP用户',
    "IsBlack" BIT DEFAULT '0' NOT NULL COMMENT '是否是黑名单用户',
    "Country" INT NOT NULL DEFAULT '0' COMMENT '国家',
    "AccountBalance" DECIMAL(18,2) DEFAULT '0.00' NOT NULL COMMENT '账户余额',
    "AccountBalance2" DECIMAL(18,2) DEFAULT '0.00' NOT NULL COMMENT '账户余额',
    "Status" INT NOT NULL DEFAULT '0' COMMENT '状态',
    "Remark" VARCHAR(255) DEFAULT NULL COMMENT '备注',
    "CreateTime" DATETIME NOT NULL COMMENT '创建时间',
    "UpdateTime" DATETIME COMMENT '更新时间',
    PRIMARY KEY ("Id")
) COMMENT '测试表';