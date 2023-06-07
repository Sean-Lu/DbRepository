CREATE TABLE "{$TableName$}" (
    "Id" SERIAL,
    "UserId" BIGINT NOT NULL,
    "UserName" TEXT NULL,
    "Age" INTEGER NOT NULL,
    "Sex" INTEGER NOT NULL,
    "PhoneNumber" TEXT NULL,
    "Email" TEXT NULL,
    "IsVip" BOOLEAN NOT NULL,
    "IsBlack" BOOLEAN NOT NULL,
    "Country" INTEGER NOT NULL,
    "AccountBalance" NUMERIC NOT NULL,
    "AccountBalance2" NUMERIC NOT NULL,
    "Status" INTEGER NOT NULL,
    "Remark" TEXT NULL,
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdateTime" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_{$TableName$}" PRIMARY KEY ("Id")
);