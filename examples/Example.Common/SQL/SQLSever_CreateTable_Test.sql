CREATE TABLE [{$TableName$}] (
  [Id] bigint NOT NULL IDENTITY,
  [UserId] bigint NOT NULL,
  [UserName] nvarchar(max) NULL,
  [Age] int NOT NULL,
  [Sex] int NOT NULL,
  [PhoneNumber] nvarchar(max) NULL,
  [Email] nvarchar(max) NULL,
  [IsVip] bit NOT NULL,
  [IsBlack] bit NOT NULL,
  [Country] int NOT NULL,
  [AccountBalance] decimal(18,2) NOT NULL,
  [AccountBalance2] decimal(18,2) NOT NULL,
  [Status] int NOT NULL,
  [Remark] nvarchar(max) NULL,
  [CreateTime] datetime2 NOT NULL,
  [UpdateTime] datetime2 NULL,
  CONSTRAINT [PK_Test] PRIMARY KEY ([Id])
);