using System;

namespace Sean.Core.DbRepository;

public static class DbContextConfiguration
{
    public static DbOptions Options => _options ??= new DbOptions();
    public static SqlServerOptions SqlServerOptions => _sqlServerOptions ??= new SqlServerOptions();

    private static DbOptions _options;
    private static SqlServerOptions _sqlServerOptions;

    public static void Configure(Action<DbOptions> action)
    {
        action?.Invoke(Options);
    }

    public static void ConfigureSqlServer(Action<SqlServerOptions> action)
    {
        action?.Invoke(SqlServerOptions);
    }
}