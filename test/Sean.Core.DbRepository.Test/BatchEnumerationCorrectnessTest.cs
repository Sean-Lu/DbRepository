using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 批量输入只枚举一次以及 SQL Builder 重复构建稳定性的回归测试。
/// </summary>
[TestClass]
public class BatchEnumerationCorrectnessTest
{
    [TestMethod]
    public void InsertBuilder_OneShotEnumerable_CanBuildRepeatedly()
    {
        var source = new OneShotEnumerable<BatchItem>(new[]
        {
            new BatchItem { Name = "第一条" },
            new BatchItem { Name = "第二条" }
        });
        var builder = SqlFactory.CreateInsertableBuilder<BatchItem>(DatabaseType.MySql)
            .SetParameter(source);

        var firstCommand = builder.Build();
        var secondCommand = builder.Build();

        Assert.AreEqual(1, source.EnumerationCount);
        Assert.AreEqual(firstCommand.Sql, secondCommand.Sql);
        Assert.AreEqual("INSERT INTO `Batch21Item`(`Name`) VALUES(@Name_1), (@Name_2)", firstCommand.Sql);
        AssertBatchParameters(firstCommand.Parameter, "第一条", "第二条");
        AssertBatchParameters(secondCommand.Parameter, "第一条", "第二条");
    }

    [TestMethod]
    public void InsertAndReplaceBuilder_EmptyOneShotEnumerable_ReturnNoCommand()
    {
        var insertSource = new OneShotEnumerable<BatchItem>(Array.Empty<BatchItem>());
        var replaceSource = new OneShotEnumerable<BatchItem>(Array.Empty<BatchItem>());

        var insertCommand = SqlFactory.CreateInsertableBuilder<BatchItem>(DatabaseType.MySql)
            .SetParameter(insertSource)
            .Build();
        var replaceCommand = SqlFactory.CreateReplaceableBuilder<BatchItem>(DatabaseType.MySql)
            .SetParameter(replaceSource)
            .Build();

        Assert.IsNull(insertCommand);
        Assert.IsNull(replaceCommand);
        Assert.AreEqual(1, insertSource.EnumerationCount);
        Assert.AreEqual(1, replaceSource.EnumerationCount);
    }

    [TestMethod]
    public void InsertBuilder_IdentityValue_DoesNotMutateBuilderFields()
    {
        var builder = SqlFactory.CreateInsertableBuilder<BatchItem>(DatabaseType.MySql);
        builder.SetParameter(new[] { new BatchItem { Id = 10, Name = "显式主键" } });

        var explicitIdentityCommand = builder.Build();
        var repeatedCommand = builder.Build();

        Assert.AreEqual(explicitIdentityCommand.Sql, repeatedCommand.Sql);
        StringAssert.Contains(explicitIdentityCommand.Sql, "(`Id`, `Name`)");
        var explicitParameters = explicitIdentityCommand.Parameter as Dictionary<string, object>;
        Assert.IsNotNull(explicitParameters);
        Assert.AreEqual(10L, explicitParameters["Id_1"]);

        builder.SetParameter(new[] { new BatchItem { Name = "数据库生成主键" } });
        var generatedIdentityCommand = builder.Build();

        Assert.AreEqual("INSERT INTO `Batch21Item`(`Name`) VALUES(@Name_1)", generatedIdentityCommand.Sql);
        Assert.IsFalse(((Dictionary<string, object>)generatedIdentityCommand.Parameter).ContainsKey("Id_1"));
    }

    [TestMethod]
    public void InsertBuilder_MutableListChangedAfterFirstBuild_DoesNotChangeSnapshot()
    {
        var entities = new List<BatchItem>
        {
            new BatchItem { Name = "快照一" }
        };
        var builder = SqlFactory.CreateInsertableBuilder<BatchItem>(DatabaseType.MySql)
            .SetParameter(entities);

        var firstCommand = builder.Build();
        entities.Add(new BatchItem { Name = "快照二" });
        var secondCommand = builder.Build();

        Assert.AreEqual(firstCommand.Sql, secondCommand.Sql);
        Assert.AreEqual("INSERT INTO `Batch21Item`(`Name`) VALUES(@Name_1)", secondCommand.Sql);
        AssertBatchParameters(secondCommand.Parameter, "快照一");
    }

    [TestMethod]
    public void InsertAndReplaceBuilder_NonParameterizedOneShotEnumerable_PreserveSqlLiteralRules()
    {
        var insertSource = new OneShotEnumerable<BatchItem>(new[]
        {
            new BatchItem { Name = "甲" },
            new BatchItem { Name = "O'Reilly" }
        });
        var insertCommand = SqlFactory.CreateInsertableBuilder<BatchItem>(DatabaseType.MySql)
            .SetParameter(insertSource)
            .SetSqlParameterized(false)
            .Build();

        var replaceSource = new OneShotEnumerable<BatchItem>(new[]
        {
            new BatchItem { Id = 1, Name = "甲" },
            new BatchItem { Id = 2, Name = "O'Reilly" }
        });
        var replaceBuilder = SqlFactory.CreateReplaceableBuilder<BatchItem>(DatabaseType.MySql)
            .SetParameter(replaceSource)
            .SetSqlParameterized(false);
        var replaceCommand = replaceBuilder.Build();
        var repeatedReplaceCommand = replaceBuilder.Build();

        Assert.AreEqual(1, insertSource.EnumerationCount);
        Assert.AreEqual("INSERT INTO `Batch21Item`(`Name`) VALUES('甲'), ('O''Reilly')", insertCommand.Sql);
        Assert.AreEqual(1, replaceSource.EnumerationCount);
        Assert.AreEqual(replaceCommand.Sql, repeatedReplaceCommand.Sql);
        Assert.AreEqual("REPLACE INTO `Batch21Item`(`Id`, `Name`) VALUES(1, '甲'), (2, 'O''Reilly')", replaceCommand.Sql);
    }

    [TestMethod]
    public void Repository_BatchMethods_EnumerateEachInputOnlyOnceAndExecuteOnSQLite()
    {
        var databasePath = CreateDatabase();
        try
        {
            var repository = new BatchRepository(GetConnectionString(databasePath), bulkEntityCount: 1);

            var emptySource = new OneShotEnumerable<BatchItem>(Array.Empty<BatchItem>());
            Assert.IsFalse(repository.Add(emptySource));
            Assert.AreEqual(1, emptySource.EnumerationCount);

            var addSource = new OneShotEnumerable<BatchItem>(new[]
            {
                new BatchItem { Name = "新增一" },
                new BatchItem { Name = "新增二" }
            });
            Assert.IsTrue(repository.Add(addSource));
            Assert.AreEqual(1, addSource.EnumerationCount);

            var rows = ReadRows(databasePath);
            var replaceSource = new OneShotEnumerable<BatchItem>(new[]
            {
                new BatchItem { Id = rows[0].Id, Name = "替换一" },
                new BatchItem { Id = rows[1].Id, Name = "替换二" }
            });
            Assert.IsTrue(repository.AddOrUpdate(replaceSource));
            Assert.AreEqual(1, replaceSource.EnumerationCount);

            var updateSource = new OneShotEnumerable<BatchItem>(new[]
            {
                new BatchItem { Id = rows[0].Id, Name = "更新一" },
                new BatchItem { Id = rows[1].Id, Name = "更新二" }
            });
            Assert.IsTrue(repository.Update(updateSource, entity => entity.Name));
            Assert.AreEqual(1, updateSource.EnumerationCount);

            var deleteSource = new OneShotEnumerable<BatchItem>(new[]
            {
                new BatchItem { Id = rows[0].Id },
                new BatchItem { Id = rows[1].Id }
            });
            Assert.IsTrue(repository.Delete(deleteSource));
            Assert.AreEqual(1, deleteSource.EnumerationCount);
            Assert.AreEqual(0, ReadRows(databasePath).Count);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public async Task Repository_AsyncBatchMethods_EnumerateEachInputOnlyOnceAndExecuteOnSQLite()
    {
        var databasePath = CreateDatabase();
        try
        {
            var repository = new BatchRepository(GetConnectionString(databasePath), bulkEntityCount: 1);
            var addSource = new OneShotEnumerable<BatchItem>(new[]
            {
                new BatchItem { Name = "异步新增一" },
                new BatchItem { Name = "异步新增二" }
            });
            Assert.IsTrue(await repository.AddAsync(addSource));
            Assert.AreEqual(1, addSource.EnumerationCount);

            var rows = ReadRows(databasePath);
            var replaceSource = new OneShotEnumerable<BatchItem>(rows.Select(row =>
                new BatchItem { Id = row.Id, Name = row.Name + "-替换" }));
            Assert.IsTrue(await repository.AddOrUpdateAsync(replaceSource));
            Assert.AreEqual(1, replaceSource.EnumerationCount);

            var updateSource = new OneShotEnumerable<BatchItem>(rows.Select(row =>
                new BatchItem { Id = row.Id, Name = row.Name + "-更新" }));
            Assert.IsTrue(await repository.UpdateAsync(updateSource, entity => entity.Name));
            Assert.AreEqual(1, updateSource.EnumerationCount);

            var deleteSource = new OneShotEnumerable<BatchItem>(rows.Select(row =>
                new BatchItem { Id = row.Id }));
            Assert.IsTrue(await repository.DeleteAsync(deleteSource));
            Assert.AreEqual(1, deleteSource.EnumerationCount);
            Assert.AreEqual(0, ReadRows(databasePath).Count);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public async Task Repository_DtoBatchMethods_EnumerateEachInputOnlyOnce()
    {
        var databasePath = CreateDatabase();
        try
        {
            var repository = new BatchRepository(GetConnectionString(databasePath));
            Assert.IsTrue(repository.Add(new[]
            {
                new BatchItem { Name = "DTO 初始一" },
                new BatchItem { Name = "DTO 初始二" }
            }));

            var rows = ReadRows(databasePath);
            var syncSource = new OneShotEnumerable<BatchDto>(rows.Select(row =>
                new BatchDto { Id = row.Id, Name = row.Name + "-同步" }));
            Assert.IsTrue(repository.UpdateByDto<BatchDto>(syncSource));
            Assert.AreEqual(1, syncSource.EnumerationCount);

            var asyncRows = ReadRows(databasePath);
            var asyncSource = new OneShotEnumerable<BatchDto>(asyncRows.Select(row =>
                new BatchDto { Id = row.Id, Name = row.Name + "-异步" }));
            Assert.IsTrue(await repository.UpdateByDtoAsync<BatchDto>(asyncSource));
            Assert.AreEqual(1, asyncSource.EnumerationCount);
            Assert.IsTrue(ReadRows(databasePath).All(row => row.Name.EndsWith("-异步", StringComparison.Ordinal)));
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public async Task Repository_SaveBatchMethods_EnumerateEachInputOnlyOnce()
    {
        var databasePath = CreateDatabase();
        try
        {
            var repository = new BatchRepository(GetConnectionString(databasePath));
            Assert.IsTrue(repository.Add(new[]
            {
                new BatchItem { Name = "保留项" },
                new BatchItem { Name = "删除项" }
            }));

            var rows = ReadRows(databasePath);
            var syncItems = new[]
            {
                new BatchItem { Name = "同步新增", EntityState = EntityStateType.Added },
                new BatchItem { Id = rows[0].Id, Name = "同步修改", EntityState = EntityStateType.Modified },
                new BatchItem { Id = rows[1].Id, Name = rows[1].Name, EntityState = EntityStateType.Deleted }
            };
            var syncSource = new OneShotEnumerable<BatchItem>(syncItems);
            Assert.IsTrue(repository.Save(syncSource));
            Assert.AreEqual(1, syncSource.EnumerationCount);
            Assert.IsTrue(syncItems.All(item => item.EntityState == EntityStateType.Unchanged));

            var asyncItems = new[]
            {
                new BatchItem { Name = "异步新增", EntityState = EntityStateType.Added }
            };
            var asyncSource = new OneShotEnumerable<BatchItem>(asyncItems);
            Assert.IsTrue(await repository.SaveAsync(asyncSource));
            Assert.AreEqual(1, asyncSource.EnumerationCount);
            Assert.AreEqual(EntityStateType.Unchanged, asyncItems[0].EntityState);

            var finalRows = ReadRows(databasePath);
            Assert.AreEqual(3, finalRows.Count);
            Assert.IsTrue(finalRows.Any(row => row.Name == "同步修改"));
            Assert.IsTrue(finalRows.Any(row => row.Name == "同步新增"));
            Assert.IsTrue(finalRows.Any(row => row.Name == "异步新增"));
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void ResetEntityState_OneShotEnumerable_EnumeratesOnlyOnce()
    {
        var source = new OneShotEnumerable<StatefulItem>(new[]
        {
            new StatefulItem { EntityState = EntityStateType.Added },
            new StatefulItem { EntityState = EntityStateType.Modified }
        });

        source.ResetEntityState();

        Assert.AreEqual(1, source.EnumerationCount);
        Assert.IsTrue(source.Items.All(item => item.EntityState == EntityStateType.Unchanged));
    }

    private static void AssertBatchParameters(object parameter, params string[] expectedNames)
    {
        var parameters = parameter as Dictionary<string, object>;
        Assert.IsNotNull(parameters);
        Assert.AreEqual(expectedNames.Length, parameters.Count);
        for (var index = 0; index < expectedNames.Length; index++)
        {
            Assert.AreEqual(expectedNames[index], parameters[$"Name_{index + 1}"]);
        }
    }

    private static string CreateDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"Sean.Core.DbRepository.Batch21.{Guid.NewGuid():N}.db");
        using var connection = new SQLiteConnection(GetConnectionString(databasePath));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE [Batch21Item] ([Id] INTEGER PRIMARY KEY AUTOINCREMENT, [Name] TEXT NOT NULL);";
        command.ExecuteNonQuery();
        return databasePath;
    }

    private static List<BatchItem> ReadRows(string databasePath)
    {
        var result = new List<BatchItem>();
        using var connection = new SQLiteConnection(GetConnectionString(databasePath));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Id], [Name] FROM [Batch21Item] ORDER BY [Id];";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new BatchItem
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1)
            });
        }
        return result;
    }

    private static string GetConnectionString(string databasePath)
    {
        return $"Data Source={databasePath};Version=3;Pooling=False;";
    }

    private static void DeleteDatabase(string databasePath)
    {
        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            var filePath = databasePath + suffix;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Table("Batch21Item")]
    private sealed class BatchItem : IEntityStateBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        [NotMapped]
        public EntityStateType EntityState { get; set; }
    }

    private sealed class BatchDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    private sealed class StatefulItem : IEntityStateBase
    {
        public EntityStateType EntityState { get; set; }
    }

    private sealed class BatchRepository : BaseRepository<BatchItem>
    {
        public BatchRepository(string connectionString, int? bulkEntityCount = null)
            : base(new ConnectionStringOptions(connectionString, SQLiteFactory.Instance)
            {
                DbType = DatabaseType.SQLite
            })
        {
            BulkEntityCount = bulkEntityCount;
        }
    }

    private sealed class OneShotEnumerable<T> : IEnumerable<T>
    {
        public OneShotEnumerable(IEnumerable<T> items)
        {
            Items = items.ToList();
        }

        public IReadOnlyList<T> Items { get; }
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new InvalidOperationException("一次性数据源不能重复枚举。");
            }
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
