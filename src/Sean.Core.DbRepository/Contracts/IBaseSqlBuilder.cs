namespace Sean.Core.DbRepository;

public interface IBaseSqlBuilder
{
    ISqlAdapter SqlAdapter { get; }
    bool SqlIndented { get; }
    bool SqlParameterized { get; }

    ISqlCommand Build();
}