using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public interface ICodeGenerator : IBaseCodeGenerator
{
    TableInfoModel GetTableInfo(string tableName);
    List<TableFieldModel> GetTableFieldInfo(string tableName);
    List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName);
}

public interface IBaseCodeGenerator
{
    void Initialize(string connectionString);
    void Initialize(DbFactory dbFactory);
}