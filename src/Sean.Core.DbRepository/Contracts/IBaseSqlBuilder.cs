namespace Sean.Core.DbRepository;

public interface IBaseSqlBuilder<out TBuild>
{
    bool SqlIndented { get; set; }
    bool SqlParameterized { get; set; }

    TBuild SetTableName(string tableName);
    TBuild SetSqlIndented(bool sqlIndented);
    TBuild SetSqlParameterized(bool sqlParameterized);

    ISqlCommand Build();
}