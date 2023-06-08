CREATE TABLE "{$TableName$}" (
    "Id" INT8 AUTO_INCREMENT NOT NULL UNIQUE,
    "UserId" INT8 NOT NULL,
    "UserName" CLOB NULL,
    "Age" INT4 NOT NULL,
    "Sex" INT4 NOT NULL,
    "PhoneNumber" CLOB NULL,
    "Email" CLOB NULL,
    "IsVip" BOOL NOT NULL,
    "IsBlack" BOOL NOT NULL,
    "Country" INT4 NOT NULL,
    "AccountBalance" DECIMAL(18,2) NOT NULL,
    "AccountBalance2" DECIMAL(18,2) NOT NULL,
    "Status" INT4 NOT NULL,
    "Remark" CLOB NULL,
    "CreateTime" TIMESTAMP NOT NULL,
    "UpdateTime" TIMESTAMP NULL,
    CONSTRAINT "PK_{$TableName$}" PRIMARY KEY ("Id")
);