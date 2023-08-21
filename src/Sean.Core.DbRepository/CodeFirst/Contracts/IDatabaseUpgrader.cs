using System;
using System.Reflection;

namespace Sean.Core.DbRepository.CodeFirst;

public interface IDatabaseUpgrader
{
    /// <summary>
    /// Upgrade the entity class to the database.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="tableNameFunc"></param>
    void Upgrade<TEntity>(Func<string, string> tableNameFunc = null);
    /// <summary>
    /// Upgrade the entity class to the database.
    /// </summary>
    /// <param name="entityType"></param>
    /// <param name="tableNameFunc"></param>
    void Upgrade(Type entityType, Func<string, string> tableNameFunc = null);

    /// <summary>
    /// Upgrade all entity classes that contain <see cref="CodeFirstAttribute"/> to the database.
    /// </summary>
    /// <param name="assemblies"></param>
    void Upgrade(params Assembly[] assemblies);
}