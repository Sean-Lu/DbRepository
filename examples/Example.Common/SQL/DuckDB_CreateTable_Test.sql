-- 通过序列的方式实现自增主键

-- ****** 因为 DuckDB 不支持一次性批量执行多个sql语句，所以下面的SQL语句需要分开单独执行 ******
CREATE SEQUENCE SQ_{$TableName$};

CREATE TABLE {$TableName$} (
  Id BIGINT PRIMARY KEY DEFAULT nextval('SQ_{$TableName$}'),
  UserId BIGINT NOT NULL,
  UserName VARCHAR(50),
  Age INTEGER NOT NULL DEFAULT 0,
  Sex TINYINT NOT NULL DEFAULT 0,
  PhoneNumber VARCHAR(50),
  Email VARCHAR(50),
  IsVip BOOLEAN NOT NULL DEFAULT FALSE,
  IsBlack BOOLEAN NOT NULL DEFAULT FALSE,
  Country INTEGER NOT NULL DEFAULT 0,
  AccountBalance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
  AccountBalance2 DECIMAL(18,2) NOT NULL DEFAULT 0.00,
  Status INTEGER NOT NULL DEFAULT 0,
  Remark VARCHAR(255),
  CreateTime TIMESTAMP NOT NULL,
  UpdateTime TIMESTAMP
);

-- 新增测试数据
-- INSERT INTO Test (UserId, UserName, Age, Sex, PhoneNumber, Email, IsVip, IsBlack, Country, AccountBalance, AccountBalance2, Status, Remark, CreateTime, UpdateTime)
-- VALUES (1, '张三', 25, 1, '1234567890', 'zhangsan@example.com', FALSE, FALSE, 1, 100.00, 100.00, 1, '测试', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);