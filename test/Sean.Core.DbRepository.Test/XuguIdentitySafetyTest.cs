using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 验证不安全的主键回写在 INSERT 前被拒绝，并用 SQLite 保护普通新增；不声明验证了真实 Xugu 驱动。
/// </summary>
[TestClass]
public class XuguIdentitySafetyTest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ReturnIdentity_IsRejectedBeforeInsertInsteadOfReadingAnotherRowsMaximum(bool asynchronous)
    {
        using var connection = OpenDatabase();
        using var transaction = connection.BeginTransaction();
        var repository = new IdentityRepository();
        var entity = new IdentityEntity { Name = "本次插入" };

        // 禁用回写必须发生在 INSERT 前，不能先写入再抛异常。
        if (asynchronous)
            await Assert.ThrowsAsync<NotSupportedException>(() => repository.AddAsync(entity, true, transaction: transaction));
        else
            Assert.Throws<NotSupportedException>(() => repository.Add(entity, true, transaction: transaction));
        Assert.AreEqual(0L, entity.Id);
        using var count = connection.CreateCommand();
        count.Transaction = transaction;
        count.CommandText = "SELECT COUNT(*) FROM XuguIdentitySafety";
        Assert.AreEqual(0L, count.ExecuteScalar(), "拒绝不安全功能前不能已执行 INSERT。");
        transaction.Rollback();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WithoutIdentityReturn_StillInserts(bool asynchronous)
    {
        using var connection = OpenDatabase();
        using var transaction = connection.BeginTransaction();
        var repository = new IdentityRepository();
        var entity = new IdentityEntity { Name = "普通新增" };
        Assert.IsTrue(asynchronous
            ? await repository.AddAsync(entity, transaction: transaction)
            : repository.Add(entity, transaction: transaction));
        Assert.AreEqual(0L, entity.Id);
        using var count = connection.CreateCommand();
        count.Transaction = transaction;
        count.CommandText = "SELECT COUNT(*) FROM XuguIdentitySafety";
        Assert.AreEqual(1L, count.ExecuteScalar());
        transaction.Rollback();
    }

    private static SQLiteConnection OpenDatabase()
    {
        var connection = new SQLiteConnection("Data Source=:memory:;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE XuguIdentitySafety(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)";
        command.ExecuteNonQuery();
        return connection;
    }

    [Table("XuguIdentitySafety")]
    private sealed class IdentityEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
    }

    private sealed class IdentityRepository : BaseRepository<IdentityEntity>
    {
        public IdentityRepository() : base(new ConnectionStringOptions("Data Source=:memory:", SQLiteFactory.Instance) { DbType = DatabaseType.Xugu }) { }
    }
}
