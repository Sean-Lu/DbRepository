CREATE TABLE `{$TableName$}` (
    `Id` COUNTER NOT NULL,
    `UserId` BIGINT NOT NULL,
    `UserName` VARCHAR(20) NULL,
    `Age` INTEGER NOT NULL,
    `Sex` SMALLINT NOT NULL,
    `PhoneNumber` VARCHAR(20) NULL,
    `Email` VARCHAR(50) NULL,
    `IsVip` SMALLINT NOT NULL,
    `IsBlack` SMALLINT NOT NULL,
    `Country` SMALLINT NOT NULL,
    `AccountBalance` DECIMAL(18,10) NOT NULL,
    `AccountBalance2` DECIMAL(18,10) NOT NULL,
    `Status` SMALLINT NOT NULL,
    `Remark` VARCHAR(255) NULL,
    `CreateTime` TIMESTAMP NOT NULL,
    `UpdateTime` TIMESTAMP NULL,
    CONSTRAINT `PK_{$TableName$}` PRIMARY KEY (`Id`)
);