using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForPostgreSql : BaseCodeGenerator, ICodeGenerator
{
    private DbFactory _db;

    public CodeGeneratorForPostgreSql() : base(DatabaseType.PostgreSql)
    {
    }
    public CodeGeneratorForPostgreSql(DatabaseType compatibleDbType) : base(compatibleDbType)
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
	table_schema AS {nameof(TableInfoModel.TableSchema)},
	table_name AS {nameof(TableInfoModel.TableName)},
	obj_description(pg_class.oid) AS {nameof(TableInfoModel.TableComment)}
FROM information_schema.tables
JOIN pg_database db ON db.datname = current_database()
LEFT JOIN pg_class ON pg_class.relname = table_name
WHERE table_name = '{tableName}'";
        return _db.Get<TableInfoModel>(sql);
    }

    public virtual List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        var sql = $@"SELECT
    c.table_schema AS {nameof(TableFieldModel.TableSchema)},
    c.table_name AS {nameof(TableFieldModel.TableName)},
    c.column_name AS {nameof(TableFieldModel.FieldName)},
    d.description AS {nameof(TableFieldModel.FieldComment)},
    c.column_default AS {nameof(TableFieldModel.FieldDefault)},
    c.data_type AS {nameof(TableFieldModel.FieldType)},
    c.numeric_precision AS {nameof(TableFieldModel.NumberPrecision)},
    c.numeric_scale AS {nameof(TableFieldModel.NumberScale)},
    c.character_maximum_length AS {nameof(TableFieldModel.StringMaxLength)},
    CASE WHEN c.is_nullable='YES' THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsNullable)},
    CASE WHEN pk.constraint_name IS NOT NULL THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsPrimaryKey)},
    CASE WHEN fk.constraint_name IS NOT NULL THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsForeignKey)},
    CASE WHEN c.is_identity='YES' THEN 1 ELSE 0 END AS {nameof(TableFieldModel.IsAutoIncrement)}
FROM information_schema.columns c
JOIN pg_database db ON db.datname = current_database()
JOIN pg_class pc ON pc.relname = c.table_name
LEFT JOIN pg_description d ON d.objoid = pc.oid AND d.objsubid = c.ordinal_position
LEFT JOIN
    (
        SELECT
            tc.table_schema,
            tc.table_name,
            kcu.column_name,
            tc.constraint_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
                                                       AND tc.table_schema = kcu.table_schema
                                                       AND tc.table_name = kcu.table_name
        WHERE tc.constraint_type = 'PRIMARY KEY'
    ) pk ON c.table_schema = pk.table_schema
           AND c.table_name = pk.table_name
           AND c.column_name = pk.column_name
LEFT JOIN
    (
        SELECT
            tc.table_schema,
            tc.table_name,
            kcu.column_name,
            tc.constraint_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
                                                       AND tc.table_schema = kcu.table_schema
                                                       AND tc.table_name = kcu.table_name
        WHERE tc.constraint_type = 'FOREIGN KEY'
    ) fk ON c.table_schema = fk.table_schema
           AND c.table_name = fk.table_name
           AND c.column_name = fk.column_name
WHERE c.table_name = '{tableName}'
ORDER BY c.ordinal_position;";
        return _db.Query<TableFieldModel>(sql);
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        var sql = $@"SELECT
    conname AS {nameof(TableFieldReferenceModel.ForeignKeyName)},
    connamespace::regnamespace::text AS {nameof(TableFieldReferenceModel.TableSchema)},
    conrelid::regclass::text AS {nameof(TableFieldReferenceModel.TableName)},
    a.attname AS {nameof(TableFieldReferenceModel.FieldName)},
    connamespace::regnamespace::text AS {nameof(TableFieldReferenceModel.ReferencedTableSchema)},
    confrelid::regclass::text AS {nameof(TableFieldReferenceModel.ReferencedTableName)},
    af.attname AS {nameof(TableFieldReferenceModel.ReferencedFieldName)}
FROM pg_constraint c
JOIN pg_database db ON db.datname = current_database()
JOIN pg_attribute a ON a.attnum = ANY (c.conkey) AND a.attrelid = c.conrelid
JOIN pg_attribute af ON af.attnum = ANY (c.confkey) AND af.attrelid = c.confrelid
WHERE contype = 'f' AND conrelid::regclass = '{tableName}'::regclass";
        return _db.Query<TableFieldReferenceModel>(sql);
    }
}