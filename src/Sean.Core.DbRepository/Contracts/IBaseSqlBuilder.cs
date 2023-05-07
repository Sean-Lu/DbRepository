namespace Sean.Core.DbRepository;

public interface IBaseSqlBuilder
{
    ISqlAdapter SqlAdapter { get; }
    bool SqlIndented { get; set; }
    bool SqlParameterized { get; set; }

    ISqlCommand Build();
}