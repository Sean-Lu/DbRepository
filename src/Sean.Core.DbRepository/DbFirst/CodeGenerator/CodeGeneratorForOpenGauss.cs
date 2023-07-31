using System;
using System.Collections.Generic;

namespace Sean.Core.DbRepository.DbFirst;

public class CodeGeneratorForOpenGauss : BaseCodeGenerator, ICodeGenerator
{
    public CodeGeneratorForOpenGauss() : base(DatabaseType.OpenGauss)
    {
    }
    public CodeGeneratorForOpenGauss(DatabaseType compatibleDbType) : base(compatibleDbType)
    {
    }

    public virtual TableInfoModel GetTableInfo(string tableName)
    {
        throw new NotImplementedException();
    }

    public virtual List<TableFieldModel> GetTableFieldInfo(string tableName)
    {
        throw new NotImplementedException();
    }

    public virtual List<TableFieldReferenceModel> GetTableFieldReferenceInfo(string tableName)
    {
        throw new NotImplementedException();
    }
}