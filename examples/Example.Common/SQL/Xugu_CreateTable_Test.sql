CREATE TABLE "{$TableName$}" (
  "Id" bigint IDENTITY(1,1) COMMENT '自增主键',
  "UserId" bigint NOT NULL COMMENT '用户id',
  "UserName" varchar(50) DEFAULT NULL COMMENT '用户名称',
  "Age" int NOT NULL DEFAULT '0' COMMENT '年龄',
  "Sex" tinyint NOT NULL DEFAULT '0' COMMENT '性别',
  "PhoneNumber" varchar(50) DEFAULT NULL COMMENT '电话号码',
  "Email" varchar(50) DEFAULT NULL COMMENT '邮箱',
  "IsVip" boolean NOT NULL DEFAULT false COMMENT '是否是VIP用户',
  "IsBlack" boolean NOT NULL DEFAULT false COMMENT '是否是黑名单用户',
  "Country" int NOT NULL DEFAULT '0' COMMENT '国家',
  "AccountBalance" decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '账户余额',
  "AccountBalance2" decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '账户余额',
  "Status" int NOT NULL DEFAULT '0' COMMENT '状态',
  "Remark" varchar(255) DEFAULT NULL COMMENT '备注',
  "CreateTime" timestamp NOT NULL COMMENT '创建时间',
  "UpdateTime" timestamp DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY ("Id")
);