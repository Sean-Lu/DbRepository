using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class InsertCommandPreparationTest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Add_PreparesSingleAndBatchedCommandsAfterEntityHooks(bool asynchronous)
    {
        using var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False;");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var repository = new RecordingRepository();
        var single = new Row { Name = "single" };
        Assert.IsTrue(asynchronous
            ? await repository.AddAsync(single, fieldExpression: row => row.Name, transaction: transaction)
            : repository.Add(single, fieldExpression: row => row.Name, transaction: transaction));
        var rows = new[] { new Row { Name = "a" }, new Row { Name = "b" }, new Row { Name = "c" } };
        Assert.IsTrue(asynchronous
            ? await repository.AddAsync(rows, fieldExpression: row => row.Name, transaction: transaction)
            : repository.Add(rows, fieldExpression: row => row.Name, transaction: transaction));

        CollectionAssert.AreEqual(new[] { "single", "batch:2", "batch:1" }, repository.Hooks);
        Assert.AreEqual(3, repository.Commands.Count);
        Assert.AreSame(single, repository.Commands[0].Parameter);
        Assert.AreEqual("single!", single.Name);
        foreach (var command in repository.Commands)
        {
            Assert.IsTrue(command.Master);
            Assert.AreSame(transaction, command.Transaction);
            Assert.AreEqual(41, command.CommandTimeout);
            StringAssert.Contains(command.Sql, "(`Name`) VALUES");
        }
        // 批量参数在 Build 时已取值，必须捕获到钩子修改后的实体，而不是提前生成命令。
        CollectionAssert.AreEqual(new object[] { "a!", "b!" }, ((IDictionary<string, object>)repository.Commands[1].Parameter).Values.ToArray());
        CollectionAssert.AreEqual(new object[] { "c!" }, ((IDictionary<string, object>)repository.Commands[2].Parameter).Values.ToArray());
        Assert.AreEqual(System.Data.ConnectionState.Open, connection.State);
        transaction.Rollback();
    }

    private sealed class Row
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private sealed class RecordingRepository : BaseRepository<Row>
    {
        internal readonly List<ISqlCommand> Commands = new();
        internal readonly List<string> Hooks = new();
        internal RecordingRepository() : base("Data Source=:memory:;Pooling=False;", SQLiteFactory.Instance)
        {
            BulkEntityCount = 2;
            CommandTimeout = 41;
        }
        protected override void BeforeEntityAdded(Row entity)
        {
            Hooks.Add("single");
            entity.Name += "!";
        }
        protected override void BeforeEntitiesAdded(IEnumerable<Row> entities)
        {
            Hooks.Add("batch:" + entities.Count());
            foreach (var entity in entities) entity.Name += "!";
        }
        public override int Execute(ISqlCommand command) { Commands.Add(command); return 1; }
        public override Task<int> ExecuteAsync(ISqlCommand command) => Task.FromResult(Execute(command));
    }
}
