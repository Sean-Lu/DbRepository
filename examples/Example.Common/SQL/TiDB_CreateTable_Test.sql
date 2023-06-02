CREATE TABLE `{$TableName$}` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `UserId` bigint NOT NULL,
    `UserName` longtext NULL,
    `Age` int NOT NULL,
    `Sex` int NOT NULL,
    `PhoneNumber` longtext NULL,
    `Email` longtext NULL,
    `IsVip` tinyint(1) NOT NULL,
    `IsBlack` tinyint(1) NOT NULL,
    `Country` int NOT NULL,
    `AccountBalance` decimal(18,2) NOT NULL,
    `AccountBalance2` decimal(18,2) NOT NULL,
    `Status` int NOT NULL,
    `Remark` longtext NULL,
    `CreateTime` datetime(6) NOT NULL,
    `UpdateTime` datetime(6) NULL,
    PRIMARY KEY (`Id`)
);