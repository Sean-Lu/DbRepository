using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForSQLite : BaseCodeGenerator, ICodeGenerator
{
    public CodeGeneratorForSQLite() : base(DatabaseType.SQLite)
    {
    }
    public CodeGeneratorForSQLite(DatabaseType compatibleDbType) : base(compatibleDbType)
    {
    }

    public virtual TableInfoModel GetTableInfo(string tableName)
    {
        return new TableInfoModel
        {
            TableName = tableName
        };
    }

    public virtual List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        // 此处表名作为字符串参数而非标识符，按标准 SQL 转义单引号。
        var escapedTableName = tableName?.Replace("'", "''");
        var sql = $@"SELECT
	`name` AS {nameof(TableFieldModel.FieldName)},
	`type` AS {nameof(TableFieldModel.FieldType)},
	`dflt_value` AS {nameof(TableFieldModel.FieldDefault)},
	NOT `notnull` AS {nameof(TableFieldModel.IsNullable)},
	`pk` AS {nameof(TableFieldModel.IsPrimaryKey)}
FROM pragma_table_info('{escapedTableName}')";
        var result = _db.Query<TableFieldModel>(sql);
        result?.ForEach(c => c.TableName = tableName);
        return result;
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        // 与字段查询保持一致，避免合法表名中的单引号截断字符串。
        var escapedTableName = tableName?.Replace("'", "''");
        var sql = $@"SELECT
	`table` AS {nameof(TableFieldReferenceModel.ReferencedTableName)},
	`from` AS {nameof(TableFieldReferenceModel.FieldName)},
	`to` AS {nameof(TableFieldReferenceModel.ReferencedFieldName)}
FROM pragma_foreign_key_list('{escapedTableName}')";
        var result = _db.Query<TableFieldReferenceModel>(sql);
        result?.ForEach(c => c.TableName = tableName);
        return result;
    }
}
