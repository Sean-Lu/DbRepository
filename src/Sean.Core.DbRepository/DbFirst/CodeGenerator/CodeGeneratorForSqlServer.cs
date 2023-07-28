using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForSqlServer : BaseCodeGenerator, ICodeGenerator
{
    private DbFactory _db;

    public CodeGeneratorForSqlServer() : base(DatabaseType.SqlServer)
    {
    }
    public CodeGeneratorForSqlServer(DatabaseType compatibleDbType) : base(compatibleDbType)
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
        var sql = $@"SELECT
	SCHEMA_NAME(t.schema_id) AS {nameof(TableInfoModel.TableSchema)},
	t.name AS {nameof(TableInfoModel.TableName)},
	Ext_TableDescription.VALUE AS {nameof(TableInfoModel.TableComment)},
	t.create_date AS {nameof(TableInfoModel.CreateTime)}
FROM sys.tables t
LEFT JOIN sys.extended_properties Ext_TableDescription ON Ext_TableDescription.MAJOR_ID = t.object_id AND Ext_TableDescription.MINOR_ID = 0 AND Ext_TableDescription.NAME = 'MS_Description'
WHERE t.NAME = '{tableName}'";
        return _db.Get<TableInfoModel>(sql);
    }

    public virtual List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        using var conn = _db.OpenNewConnection();
        var sql = $@"SELECT 
    SCHEMA_NAME(t.schema_id) AS {nameof(TableFieldModel.TableSchema)},
    t.name AS {nameof(TableFieldModel.TableName)},
    c.name AS {nameof(TableFieldModel.FieldName)},
    ep.value AS {nameof(TableFieldModel.FieldComment)},
    OBJECT_DEFINITION(c.default_object_id) AS {nameof(TableFieldModel.FieldDefault)},
    tp.name AS {nameof(TableFieldModel.FieldType)},
    c.precision AS {nameof(TableFieldModel.NumberPrecision)},
    c.scale AS {nameof(TableFieldModel.NumberScale)},
    col.CHARACTER_MAXIMUM_LENGTH AS {nameof(TableFieldModel.StringMaxLength)},
    c.is_nullable AS {nameof(TableFieldModel.IsNullable)},
    CASE WHEN pk.name IS NOT NULL THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsPrimaryKey)},
    CASE WHEN fk.name IS NOT NULL THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsForeignKey)},
    c.is_identity AS {nameof(TableFieldModel.IsAutoIncrement)}
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN INFORMATION_SCHEMA.COLUMNS col ON col.TABLE_CATALOG = '{conn.Database}' AND col.TABLE_SCHEMA = SCHEMA_NAME(t.schema_id) AND col.TABLE_NAME = t.name AND col.COLUMN_NAME = c.name
INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
LEFT JOIN sys.extended_properties ep ON ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = 'MS_Description'
LEFT JOIN sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
LEFT JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
LEFT JOIN sys.key_constraints pk ON ic.object_id = pk.parent_object_id AND ic.column_id = pk.unique_index_id AND i.is_primary_key = 1
LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
LEFT JOIN sys.key_constraints fk ON fkc.constraint_object_id = fk.object_id
WHERE t.name='{tableName}'
ORDER BY col.ORDINAL_POSITION";
        return _db.Query<TableFieldModel>(conn, sql);
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        var sql = $@"SELECT 
    FK.name AS {nameof(TableFieldReferenceModel.ForeignKeyName)},
    SCHEMA_NAME(TP.schema_id) AS {nameof(TableFieldReferenceModel.TableSchema)},
    TP.name AS {nameof(TableFieldReferenceModel.TableName)},
    CP.name AS {nameof(TableFieldReferenceModel.FieldName)},
    SCHEMA_NAME(TC.schema_id) AS {nameof(TableFieldReferenceModel.ReferencedTableSchema)},
    TC.name AS {nameof(TableFieldReferenceModel.ReferencedTableName)},
    CC.name AS {nameof(TableFieldReferenceModel.ReferencedFieldName)}
FROM sys.foreign_keys FK
INNER JOIN sys.tables TP ON FK.parent_object_id = TP.object_id
INNER JOIN sys.tables TC ON FK.referenced_object_id = TC.object_id
INNER JOIN sys.foreign_key_columns FKC ON FKC.constraint_object_id = FK.object_id
INNER JOIN sys.columns CP ON FKC.parent_column_id = CP.column_id AND FKC.parent_object_id = CP.object_id
INNER JOIN sys.columns CC ON FKC.referenced_column_id = CC.column_id AND FKC.referenced_object_id = CC.object_id
WHERE TP.name = '{tableName}'";
        return _db.Query<TableFieldReferenceModel>(sql);
    }
}