using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForFirebird : BaseCodeGenerator, ICodeGenerator
{
    public CodeGeneratorForFirebird() : base(DatabaseType.Firebird)
    {
    }
    public CodeGeneratorForFirebird(DatabaseType compatibleDbType) : base(compatibleDbType)
    {
    }

    public virtual TableInfoModel GetTableInfo(string tableName)
    {
        var sql = $@"SELECT
    TRIM(RDB$OWNER_NAME) AS ""{nameof(TableInfoModel.TableSchema)}"",
    TRIM(RDB$RELATION_NAME) AS ""{nameof(TableInfoModel.TableName)}"",
    RDB$DESCRIPTION AS ""{nameof(TableInfoModel.TableComment)}""
FROM RDB$RELATIONS
WHERE RDB$VIEW_BLR IS NULL
    AND (RDB$SYSTEM_FLAG IS NULL OR RDB$SYSTEM_FLAG = 0)
    AND RDB$RELATION_NAME='{tableName}'";
        return _db.Get<TableInfoModel>(sql);
    }

    public virtual List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        var sql = $@"SELECT
    TRIM(r.RDB$OWNER_NAME) AS ""{nameof(TableFieldModel.TableSchema)}"",
    TRIM(rf.RDB$RELATION_NAME) AS ""{nameof(TableFieldModel.TableName)}"",
    TRIM(rf.RDB$FIELD_NAME) AS ""{nameof(TableFieldModel.FieldName)}"",
    rf.RDB$DESCRIPTION AS ""{nameof(TableFieldModel.FieldComment)}"",
    rf.RDB$DEFAULT_SOURCE AS ""{nameof(TableFieldModel.FieldDefault)}"",
    TRIM(t.RDB$TYPE_NAME) AS ""{nameof(TableFieldModel.FieldType)}"",
    f.RDB$FIELD_PRECISION AS ""{nameof(TableFieldModel.NumericPrecision)}"",
    f.RDB$FIELD_SCALE AS ""{nameof(TableFieldModel.NumericScale)}"",
    f.RDB$CHARACTER_LENGTH AS ""{nameof(TableFieldModel.StringMaxLength)}"",
    CASE WHEN rf.RDB$NULL_FLAG = 1 THEN 0 ELSE 1 END AS ""{nameof(TableFieldModel.IsNullable)}"",
    CASE WHEN EXISTS (
            SELECT 1
            FROM RDB$RELATION_CONSTRAINTS rc
            JOIN RDB$INDEX_SEGMENTS isg ON rc.RDB$INDEX_NAME = isg.RDB$INDEX_NAME
            WHERE rc.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
            AND rc.RDB$RELATION_NAME = rf.RDB$RELATION_NAME
            AND isg.RDB$FIELD_NAME = rf.RDB$FIELD_NAME
        ) THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsPrimaryKey)}"",
    CASE WHEN EXISTS (
            SELECT 1
            FROM RDB$RELATION_CONSTRAINTS rc
            JOIN RDB$REF_CONSTRAINTS ref ON rc.RDB$CONSTRAINT_NAME = ref.RDB$CONSTRAINT_NAME
            WHERE rc.RDB$RELATION_NAME = rf.RDB$RELATION_NAME
            AND ref.RDB$CONST_NAME_UQ = rf.RDB$FIELD_NAME
        ) THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsForeignKey)}"",
    CASE WHEN rf.RDB$GENERATOR_NAME IS NOT NULL THEN 1 ELSE 0 END AS ""{nameof(TableFieldModel.IsAutoIncrement)}""
FROM RDB$RELATIONS r
JOIN RDB$RELATION_FIELDS rf ON r.RDB$RELATION_NAME = rf.RDB$RELATION_NAME
JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
JOIN RDB$TYPES t ON f.RDB$FIELD_TYPE = t.RDB$TYPE AND t.RDB$FIELD_NAME = 'RDB$FIELD_TYPE'
WHERE r.RDB$SYSTEM_FLAG = 0
    AND r.RDB$VIEW_BLR IS NULL
    AND r.RDB$RELATION_NAME = '{tableName}'
ORDER BY rf.RDB$FIELD_POSITION";
        return _db.Query<TableFieldModel>(sql);
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        var sql = $@"SELECT
    TRIM(rc.RDB$CONSTRAINT_NAME) AS ""{nameof(TableFieldReferenceModel.ForeignKeyName)}"",
    TRIM(r.RDB$OWNER_NAME) AS ""{nameof(TableFieldReferenceModel.TableSchema)}"",
    TRIM(r.RDB$RELATION_NAME) AS ""{nameof(TableFieldReferenceModel.TableName)}"",
    TRIM(s.RDB$FIELD_NAME) AS ""{nameof(TableFieldReferenceModel.FieldName)}"",
    TRIM(r.RDB$OWNER_NAME) AS ""{nameof(TableFieldReferenceModel.ReferencedTableSchema)}"",
    TRIM(ri.RDB$RELATION_NAME) AS ""{nameof(TableFieldReferenceModel.ReferencedTableName)}"",
    TRIM(si.RDB$FIELD_NAME) AS ""{nameof(TableFieldReferenceModel.ReferencedFieldName)}""
FROM RDB$RELATION_CONSTRAINTS rc
JOIN RDB$INDEX_SEGMENTS s ON rc.RDB$INDEX_NAME = s.RDB$INDEX_NAME
JOIN RDB$REF_CONSTRAINTS ref ON rc.RDB$CONSTRAINT_NAME = ref.RDB$CONSTRAINT_NAME
JOIN RDB$RELATION_CONSTRAINTS ri ON ref.RDB$CONST_NAME_UQ = ri.RDB$CONSTRAINT_NAME
JOIN RDB$INDEX_SEGMENTS si ON ri.RDB$INDEX_NAME = si.RDB$INDEX_NAME
JOIN RDB$RELATIONS r ON rc.RDB$RELATION_NAME = r.RDB$RELATION_NAME
WHERE rc.RDB$CONSTRAINT_TYPE = 'FOREIGN KEY'
    AND rc.RDB$RELATION_NAME = '{tableName}'";
        return _db.Query<TableFieldReferenceModel>(sql);
    }
}