using System;
using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForMsAccess : BaseCodeGenerator, ICodeGenerator
{
    public CodeGeneratorForMsAccess() : base(DatabaseType.MsAccess)
    {
    }
    public CodeGeneratorForMsAccess(DatabaseType compatibleDbType) : base(compatibleDbType)
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
        var result = new List<TableFieldModel>();

        using var connection = _db.OpenNewConnection();
        var listFieldName = _db.Query<string>(connection, $"SELECT Name FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}')");
        foreach (var fieldName in listFieldName)
        {
            var tableFieldModel = new TableFieldModel
            {
                TableName = tableName,
                FieldName = fieldName
            };
            tableFieldModel.FieldDefault = _db.Get<string>(connection, $"SELECT DefaultValue FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}'");
            tableFieldModel.FieldType = _db.Get<string>(connection, $"SELECT Type FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}'");
            var numberTuple = _db.Get<Tuple<int?, int?>>(connection, $"SELECT NumericPrecision, NumericScale FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}' AND Type IN (3, 4, 5, 6, 7, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30)");
            tableFieldModel.NumberPrecision = numberTuple.Item1;
            tableFieldModel.NumberScale = numberTuple.Item2;
            tableFieldModel.StringMaxLength = _db.Get<int?>(connection, $"SELECT Length FROM MSysColumns WHERE ID = (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}' AND Type IN (10, 11, 12, 16, 17)");
            tableFieldModel.IsNullable = !_db.Get<bool>(connection, $"SELECT Required FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}'");
            tableFieldModel.IsPrimaryKey = _db.Get<int>(connection, $"SELECT Count(*) FROM MSysIndexes WHERE Name = 'PrimaryKey' AND IndexID = (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND ColumnID = (SELECT ID FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}')") > 0;
            tableFieldModel.IsForeignKey = _db.Get<int>(connection, $"SELECT Count(*) FROM MSysRelations WHERE TableID = (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND ForeignTableID IS NOT NULL AND ForeignColumnID = (SELECT ID FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}')") > 0;
            tableFieldModel.IsAutoIncrement = _db.Get<int>(connection, $"SELECT Count(*) FROM MSysColumns WHERE ID IN (SELECT ID FROM MSysObjects WHERE Name = '{tableName}') AND Name = '{fieldName}' AND Flags = 90") > 0;
            result.Add(tableFieldModel);
        }
        return result;

    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        var sql = $@"SELECT 
    [PrimaryTable].[Name] AS [{nameof(TableFieldReferenceModel.TableName)}],
    [PrimaryColumn].[Name] AS [{nameof(TableFieldReferenceModel.FieldName)}],
    [ForeignTable].[Name] AS [{nameof(TableFieldReferenceModel.ReferencedTableName)}],
    [ForeignColumn].[Name] AS [{nameof(TableFieldReferenceModel.ReferencedFieldName)}]
FROM MSysRelationships 
INNER JOIN MSysObjects AS PrimaryTable ON [PrimaryTable].[Id] = [MSysRelationships].[TableID]
INNER JOIN MSysObjects AS ForeignTable ON [ForeignTable].[Id] = [MSysRelationships].[ForeignTableID]
INNER JOIN MSysColumns AS PrimaryColumn ON [PrimaryColumn].[ColId] = [MSysRelationships].[PrimaryColID] AND [PrimaryColumn].[TableId] = [PrimaryTable].[Id]
INNER JOIN MSysColumns AS ForeignColumn ON [ForeignColumn].[ColId] = [MSysRelationships].[ForeignColID] AND [ForeignColumn].[TableId] = [ForeignTable].[Id]
WHERE [PrimaryTable].[Name] = '{tableName}'";
        return _db.Query<TableFieldReferenceModel>(sql);
    }
}