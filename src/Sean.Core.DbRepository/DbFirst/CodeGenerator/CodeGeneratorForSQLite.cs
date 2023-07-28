using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForSQLite : BaseCodeGenerator, ICodeGenerator
{
    private DbFactory _db;

    public CodeGeneratorForSQLite() : base(DatabaseType.SQLite)
    {
    }
    public CodeGeneratorForSQLite(DatabaseType compatibleDbType) : base(compatibleDbType)
    {
    }

    public virtual void Initialize(string connectionString)
    {
        _db = new DbFactory(new MultiConnectionSettings(ConnectionStringOptions.Create(connectionString, _dbType)));
    }

    public virtual void Initialize(DbFactory dbFactory)
    {
        _db = dbFactory;
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
        var sql = $@"SELECT
	`name` AS {nameof(TableFieldModel.FieldName)},
	`type` AS {nameof(TableFieldModel.FieldType)},
	`dflt_value` AS {nameof(TableFieldModel.FieldDefault)},
	NOT `notnull` AS {nameof(TableFieldModel.IsNullable)},
	`pk` AS {nameof(TableFieldModel.IsPrimaryKey)}
FROM pragma_table_info('{tableName}')";
        var result = _db.Query<TableFieldModel>(sql);
        result?.ForEach(c => c.TableName = tableName);
        return result;
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        var sql = $@"SELECT
	`table` AS {nameof(TableFieldReferenceModel.ReferencedTableName)},
	`from` AS {nameof(TableFieldReferenceModel.FieldName)},
	`to` AS {nameof(TableFieldReferenceModel.ReferencedFieldName)}
FROM pragma_foreign_key_list('{tableName}')";
        var result = _db.Query<TableFieldReferenceModel>(sql);
        result?.ForEach(c => c.TableName = tableName);
        return result;
    }
}