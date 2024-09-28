namespace Sean.Core.DbRepository;

public interface IBaseSqlBuilder<out TBuild>
{
    ISqlAdapter SqlAdapter { get; }
    bool SqlIndented { get; set; }
    bool SqlParameterized { get; set; }

    TBuild SetSqlIndented(bool sqlIndented);
    TBuild SetSqlParameterized(bool sqlParameterized);

    ISqlCommand Build();
}