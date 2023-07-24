using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForOracle : BaseCodeGenerator, ICodeGenerator
{
    private DbFactory _db;

    public CodeGeneratorForOracle() : base(DatabaseType.Oracle)
    {
    }
    public CodeGeneratorForOracle(DatabaseType compatibleDbType) : base(compatibleDbType)
    {
    }

    public virtual void Initialize(string connectionString)
    {
        _db = new DbFactory(new MultiConnectionSettings(ConnectionStringOptions.Create(connectionString, _dbType)));
    }

    public virtual TableInfoModel GetTableInfo(string tableName)
    {
        var sql = $@"SELECT
    ut.table_name AS {nameof(TableInfoModel.TableName)},
    utc.comments AS {nameof(TableInfoModel.TableComment)},
    uo.created AS {nameof(TableInfoModel.CreateTime)}
FROM user_tables ut
LEFT JOIN user_tab_comments utc ON ut.table_name = utc.table_name
LEFT JOIN user_objects uo ON ut.table_name = uo.object_name
WHERE ut.table_name = '{tableName}' AND uo.object_type = 'TABLE'";
        return _db.Get<TableInfoModel>(sql);
    }

    public virtual List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        var sql = $@"SELECT
    -- utc.owner AS ""{nameof(TableFieldModel.TableSchema)}"",
    utc.table_name AS ""{nameof(TableFieldModel.TableName)}"",
    utc.column_name AS ""{nameof(TableFieldModel.FieldName)}"",
    ucc.comments AS ""{nameof(TableFieldModel.FieldComment)}"",
    utc.data_default AS ""{nameof(TableFieldModel.FieldDefault)}"",
    utc.data_type AS ""{nameof(TableFieldModel.FieldType)}"",
    utc.data_precision AS ""{nameof(TableFieldModel.NumberPrecision)}"",
    utc.data_scale AS ""{nameof(TableFieldModel.NumberScale)}"",
    utc.char_length AS ""{nameof(TableFieldModel.StringMaxLength)}"",
    CASE WHEN utc.nullable = 'Y' THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsNullable)}"",
    CASE WHEN ucc.constraint_type = 'P' THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsPrimaryKey)}"",
    CASE WHEN ucc.constraint_type = 'R' THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsForeignKey)}"",
    CASE WHEN utc.identity_column = 'YES' THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsAutoIncrement)}""
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
        uc.constraint_type IN ('P', 'R')) ucc ON utc.table_name = ucc.table_name AND utc.column_name = ucc.column_name
WHERE utc.table_name = '{tableName}'
ORDER BY utc.column_id";
        return _db.Query<TableFieldModel>(sql);
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        var sql = $@"SELECT
    uc.constraint_name AS {nameof(TableFieldReferenceModel.ForeignKeyName)},
    uc.table_name AS {nameof(TableFieldReferenceModel.TableName)},
    ucc.column_name AS {nameof(TableFieldReferenceModel.FieldName)},
    ur.table_name AS {nameof(TableFieldReferenceModel.ReferencedTableName)},
    urc.column_name AS {nameof(TableFieldReferenceModel.ReferencedFieldName)}
FROM user_constraints uc
JOIN user_cons_columns ucc ON uc.constraint_name = ucc.constraint_name
JOIN user_constraints ur ON uc.r_constraint_name = ur.constraint_name
JOIN user_cons_columns urc ON ur.constraint_name = urc.constraint_name
WHERE uc.constraint_type = 'R' AND uc.table_name = '{tableName}'";
        return _db.Query<TableFieldReferenceModel>(sql);
    }
}