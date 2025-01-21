using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForDameng : CodeGeneratorForOracle
{
    public CodeGeneratorForDameng() : base(DatabaseType.Dameng)
    {
    }
    public CodeGeneratorForDameng(DatabaseType compatibleDbType) : base(compatibleDbType)
    {
    }

    public override List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        var sql = $@"SELECT
    -- utc.owner AS ""{nameof(TableFieldModel.TableSchema)}"",-- all_tab_columns.owner
    utc.table_name AS ""{nameof(TableFieldModel.TableName)}"",
    utc.column_name AS ""{nameof(TableFieldModel.FieldName)}"",
    ucc.comments AS ""{nameof(TableFieldModel.FieldComment)}"",
    utc.data_default AS ""{nameof(TableFieldModel.FieldDefault)}"",
    utc.data_type AS ""{nameof(TableFieldModel.FieldType)}"",
    utc.data_precision AS ""{nameof(TableFieldModel.NumericPrecision)}"",
    utc.data_scale AS ""{nameof(TableFieldModel.NumericScale)}"",
    utc.char_length AS ""{nameof(TableFieldModel.StringMaxLength)}"",
    CASE WHEN utc.nullable = 'Y' THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsNullable)}"",
    CASE WHEN uco.constraint_type = 'P' THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsPrimaryKey)}"",
    CASE WHEN uco.constraint_type = 'R' THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsForeignKey)}"",
    uta.IsAutoIncrement AS ""{nameof(TableFieldModel.IsAutoIncrement)}""
FROM
    user_tab_columns utc
LEFT JOIN
    user_col_comments ucc ON utc.table_name = ucc.table_name AND utc.column_name = ucc.column_name
LEFT JOIN
    (SELECT
        uc.table_name,
        ucc.column_name,
        uc.constraint_type
    FROM
        user_cons_columns ucc
    INNER JOIN
        user_constraints uc ON ucc.constraint_name = uc.constraint_name
    WHERE
        uc.constraint_type IN ('P', 'R') AND uc.table_name='{tableName}') uco ON utc.column_name = uco.column_name
LEFT JOIN
    (SELECT 
        -- SYSOBJECTS.NAME AS table_name,
        SYSCOLUMNS.NAME AS column_name,
        CASE WHEN SYSCOLUMNS.INFO2 & 0x01 = 0x01 THEN 1 ELSE 0 END AS IsAutoIncrement
     FROM SYSCOLUMNS
     INNER JOIN SYSOBJECTS ON SYSCOLUMNS.ID = SYSOBJECTS.ID 
     WHERE SYSOBJECTS.NAME = '{tableName}') uta ON utc.column_name = uta.column_name
WHERE utc.table_name = '{tableName}'
ORDER BY utc.column_id";
        return _db.Query<TableFieldModel>(sql);
    }
}