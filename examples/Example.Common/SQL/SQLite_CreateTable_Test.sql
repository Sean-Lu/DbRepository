CREATE TABLE `{$TableName$}` (
  `Id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, -- COMMENT '自增主键',
  `UserId` INTEGER NOT NULL, -- COMMENT '用户id',
  `UserName` TEXT DEFAULT NULL, -- COMMENT '用户名称',
  `Age` INTEGER NOT NULL DEFAULT 0, -- COMMENT '年龄',
  `Sex` INTEGER NOT NULL DEFAULT 0, -- COMMENT '性别',
  `PhoneNumber` TEXT DEFAULT NULL, -- COMMENT '电话号码',
  `Email` TEXT DEFAULT NULL, -- COMMENT '邮箱',
  `IsVip` INTEGER NOT NULL DEFAULT 0, -- COMMENT '是否是VIP用户',
  `IsBlack` INTEGER NOT NULL DEFAULT 0, -- COMMENT '是否是黑名单用户',
  `Country` INTEGER NOT NULL DEFAULT 0, -- COMMENT '国家',
  `AccountBalance` decimal(18,2) NOT NULL DEFAULT 0, -- COMMENT '账户余额',
  `AccountBalance2` decimal(18,2) NOT NULL DEFAULT 0, -- COMMENT '账户余额',
  `Status` INTEGER NOT NULL DEFAULT 0, -- COMMENT '状态',
  `Remark` TEXT DEFAULT NULL, -- COMMENT '备注',
  `CreateTime` TEXT NOT NULL, -- COMMENT '创建时间',
  `UpdateTime` TEXT -- COMMENT '更新时间'
);