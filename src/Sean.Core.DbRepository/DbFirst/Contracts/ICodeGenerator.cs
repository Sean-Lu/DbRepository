using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public interface ICodeGenerator
{
    void Initialize(string connectionString);

    TableInfoModel GetTableInfo(string tableName);
    List<TableFieldModel> GetTableFieldInfo(string tableName);
    List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName);
}