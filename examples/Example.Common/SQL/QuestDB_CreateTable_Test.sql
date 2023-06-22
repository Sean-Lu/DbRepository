CREATE TABLE "{$TableName$}" (
    "Id" LONG NOT NULL,
    "UserId" LONG NOT NULL,
    "UserName" STRING,
    "Age" INT NOT NULL,
    "Sex" INT NOT NULL,
    "PhoneNumber" STRING,
    "Email" STRING,
    "IsVip" BOOLEAN NOT NULL,
    "IsBlack" BOOLEAN NOT NULL,
    "Country" INT NOT NULL,
    "AccountBalance" DOUBLE NOT NULL,
    "AccountBalance2" DOUBLE NOT NULL,
    "Status" INT NOT NULL,
    "Remark" STRING,
    "CreateTime" TIMESTAMP NOT NULL,
    "UpdateTime" TIMESTAMP
);