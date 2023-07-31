using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForMySql : BaseCodeGenerator, ICodeGenerator
{
    public CodeGeneratorForMySql() : base(DatabaseType.MySql)
    {
    }
    public CodeGeneratorForMySql(DatabaseType compatibleDbType) : base(compatibleDbType)
    {
    }

    public virtual TableInfoModel GetTableInfo(string tableName)
    {
        using var conn = _db.OpenNewConnection();
        var sql = $@"SELECT
	TABLE_SCHEMA AS {nameof(TableInfoModel.TableSchema)},
	TABLE_NAME AS {nameof(TableInfoModel.TableName)},
	TABLE_COMMENT AS {nameof(TableInfoModel.TableComment)},
	CREATE_TIME AS {nameof(TableInfoModel.CreateTime)}
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = '{conn.Database}' AND TABLE_NAME = '{tableName}'";
        return _db.Get<TableInfoModel>(conn, sql);
    }

    public virtual List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        using var conn = _db.OpenNewConnection();
        var sql = $@"SELECT
	TABLE_SCHEMA AS {nameof(TableFieldModel.TableSchema)},
	TABLE_NAME AS {nameof(TableFieldModel.TableName)},
	COLUMN_NAME AS {nameof(TableFieldModel.FieldName)},
	COLUMN_COMMENT AS {nameof(TableFieldModel.FieldComment)},
	COLUMN_DEFAULT AS {nameof(TableFieldModel.FieldDefault)},
	DATA_TYPE AS {nameof(TableFieldModel.FieldType)},
    NUMERIC_PRECISION AS {nameof(TableFieldModel.NumberPrecision)},
    NUMERIC_SCALE AS {nameof(TableFieldModel.NumberScale)},
    CHARACTER_MAXIMUM_LENGTH AS {nameof(TableFieldModel.StringMaxLength)},
	CASE WHEN IS_NULLABLE='YES' THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsNullable)},
	CASE WHEN COLUMN_KEY='PRI' THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsPrimaryKey)},
	CASE WHEN COLUMN_KEY='MUL' THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsForeignKey)},
	CASE WHEN EXTRA='auto_increment' THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsAutoIncrement)}
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = '{conn.Database}' AND TABLE_NAME = '{tableName}'
ORDER BY ORDINAL_POSITION";
        return _db.Query<TableFieldModel>(conn, sql);
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        using var conn = _db.OpenNewConnection();
        var sql = $@"SELECT
	CONSTRAINT_NAME AS {nameof(TableFieldReferenceModel.ForeignKeyName)},
	TABLE_SCHEMA AS {nameof(TableFieldReferenceModel.TableSchema)},
	TABLE_NAME AS {nameof(TableFieldReferenceModel.TableName)},
	COLUMN_NAME AS {nameof(TableFieldReferenceModel.FieldName)},
	REFERENCED_TABLE_SCHEMA AS {nameof(TableFieldReferenceModel.ReferencedTableSchema)},
	REFERENCED_TABLE_NAME AS {nameof(TableFieldReferenceModel.ReferencedTableName)},
	REFERENCED_COLUMN_NAME AS {nameof(TableFieldReferenceModel.ReferencedFieldName)}
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = '{conn.Database}' AND TABLE_NAME = '{tableName}' AND CONSTRAINT_NAME <> 'PRIMARY' AND REFERENCED_TABLE_NAME IS NOT NULL";
        return _db.Query<TableFieldReferenceModel>(conn, sql);
    }
}