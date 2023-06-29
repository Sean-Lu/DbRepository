CREATE TABLE [{$TableName$}] (
  [Id] bigint NOT NULL IDENTITY,
  [UserId] bigint NOT NULL,
  [UserName] nvarchar(50) NULL,
  [Age] int NOT NULL,
  [Sex] int NOT NULL,
  [PhoneNumber] nvarchar(50) NULL,
  [Email] nvarchar(50) NULL,
  [IsVip] bit NOT NULL,
  [IsBlack] bit NOT NULL,
  [Country] int NOT NULL,
  [AccountBalance] decimal(18,2) NOT NULL,
  [AccountBalance2] decimal(18,2) NOT NULL,
  [Status] int NOT NULL,
  [Remark] nvarchar(255) NULL,
  [CreateTime] datetime2 NOT NULL,
  [UpdateTime] datetime2 NULL,
  CONSTRAINT [PK_{$TableName$}] PRIMARY KEY ([Id])
);