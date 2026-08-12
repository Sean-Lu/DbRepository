using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// DataReader、DataRow 与 DataTable 实体映射规则的回归测试。
/// </summary>
[TestClass]
public class EntityMappingCorrectnessTest
{
    [TestMethod]
    public void DataReader_ColumnAttributeTakesPriorityAndPropertyNameRemainsFallback()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    'Column优先' AS Name,
    '大小写匹配' AS mixedcase,
    '忽略列' AS UnknownColumn;";
        using var reader = command.ExecuteReader();

        var entity = reader.Get<ConflictingMappingEntity>();

        Assert.IsNotNull(entity);
        Assert.AreEqual("Column优先", entity.DisplayName);
        Assert.IsNull(entity.Name);
        Assert.AreEqual("大小写匹配", entity.MixedCase);
        Assert.AreEqual("只读初始值", entity.ReadOnlyValue);

        using var fallbackCommand = connection.CreateCommand();
        fallbackCommand.CommandText = "SELECT '属性名回退' AS DisplayName;";
        using var fallbackReader = fallbackCommand.ExecuteReader();
        var fallback = fallbackReader.Get<MappingEntity>();
        Assert.AreEqual("属性名回退", fallback.DisplayName);
    }

    [TestMethod]
    public void DataReader_ConvertsSupportedTypesAndKeepsDefaultsForDbNull()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    2 AS Status,
    42 AS NullableNumber,
    '2026-08-13 12:34:56' AS OccurredAt,
    NULL AS NullText;";
        using var reader = command.ExecuteReader();

        var entity = reader.Get<ConversionEntity>();

        Assert.AreEqual(MappingStatus.Completed, entity.Status);
        Assert.AreEqual(42, entity.NullableNumber);
        Assert.AreEqual(Guid.Empty, entity.UniqueId);
        Assert.AreEqual(new DateTime(2026, 8, 13, 12, 34, 56), entity.OccurredAt);
        Assert.AreEqual("默认文本", entity.NullText);
    }

    [TestMethod]
    public void DataReader_DuplicateColumnsPreserveLastValueBehavior()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT '第一值' AS DisplayName, '第二值' AS DisplayName;";
        using var reader = command.ExecuteReader();

        var entity = reader.Get<MappingEntity>();

        Assert.AreEqual("第二值", entity.DisplayName);
    }

    [TestMethod]
    public void DataReader_TupleValueTupleScalarAndDynamicKeepExistingSemantics()
    {
        using var connection = OpenConnection();

        using (var tupleCommand = connection.CreateCommand())
        {
            tupleCommand.CommandText = "SELECT 7, '元组', NULL;";
            using var reader = tupleCommand.ExecuteReader();
            var tuple = reader.Get<Tuple<long, string, int>>();
            Assert.AreEqual(7L, tuple.Item1);
            Assert.AreEqual("元组", tuple.Item2);
            Assert.AreEqual(0, tuple.Item3);
        }

        using (var valueTupleCommand = connection.CreateCommand())
        {
            valueTupleCommand.CommandText = "SELECT 8, '值元组';";
            using var reader = valueTupleCommand.ExecuteReader();
            var tuple = reader.Get<(int, string)>();
            Assert.AreEqual(8, tuple.Item1);
            Assert.AreEqual("值元组", tuple.Item2);
        }

        using (var scalarCommand = connection.CreateCommand())
        {
            scalarCommand.CommandText = "SELECT 9;";
            using var reader = scalarCommand.ExecuteReader();
            Assert.AreEqual(9L, reader.Get<long>());
        }

        using (var dynamicCommand = connection.CreateCommand())
        {
            dynamicCommand.CommandText = "SELECT 10 AS Id, '动态值' AS Name;";
            using var reader = dynamicCommand.ExecuteReader();
            dynamic value = reader.Get<object>();
            Assert.IsNotNull(value);
        }
    }

    [TestMethod]
    public async Task DataReaderAsync_UsesTheSameColumnMappingRules()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT '异步Column' AS DB_DISPLAY_NAME, '异步回退' AS Name
UNION ALL
SELECT '异步Column2' AS DB_DISPLAY_NAME, '异步回退2' AS Name;";
        using var reader = await command.ExecuteReaderAsync();

        var entities = await reader.GetListAsync<MappingEntity>();

        Assert.AreEqual(2, entities.Count);
        Assert.AreEqual("异步Column", entities[0].DisplayName);
        Assert.AreEqual("异步回退", entities[0].Name);
        Assert.AreEqual("异步Column2", entities[1].DisplayName);
        Assert.AreEqual("异步回退2", entities[1].Name);
    }

    [TestMethod]
    public void DataRowAndDataTable_UseColumnAttributeFallbackAndTypeConversion()
    {
        var id = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add("DB_DISPLAY_NAME", typeof(string));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Status", typeof(int));
        table.Columns.Add("NullableNumber", typeof(int));
        table.Columns.Add("UniqueId", typeof(Guid));
        table.Columns.Add("OccurredAt", typeof(string));
        table.Columns.Add("NullText", typeof(string));
        table.Columns.Add("UnknownColumn", typeof(string));
        table.Rows.Add("DataRow Column", "DataRow 回退", 1, 3, id,
            "2026-08-13 01:02:03", DBNull.Value, "忽略");
        table.Rows.Add("第二行 Column", "第二行回退", 2, DBNull.Value, Guid.Empty,
            "2026-08-14 02:03:04", "非空", "忽略");

        var mapping = table.Rows[0].ToEntity<CombinedMappingEntity>();
        var list = table.ToList<CombinedMappingEntity>();
        var first = table.ToEntity<CombinedMappingEntity>();

        Assert.AreEqual("DataRow Column", mapping.DisplayName);
        Assert.AreEqual("DataRow 回退", mapping.Name);
        Assert.AreEqual(MappingStatus.Started, mapping.Status);
        Assert.AreEqual(3, mapping.NullableNumber);
        Assert.AreEqual(id, mapping.UniqueId);
        Assert.AreEqual(new DateTime(2026, 8, 13, 1, 2, 3), mapping.OccurredAt);
        Assert.AreEqual("默认文本", mapping.NullText);
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("第二行 Column", list[1].DisplayName);
        Assert.AreEqual(MappingStatus.Completed, list[1].Status);
        Assert.IsNull(list[1].NullableNumber);
        Assert.AreEqual("DataRow Column", first.DisplayName);
    }

    [TestMethod]
    public void EmptyReaderAndDataTable_PreserveEmptyResultBehavior()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 WHERE 1 = 0;";
        using var reader = command.ExecuteReader();

        var unsupported = reader.GetList<UnsupportedMapping>();
        var table = new DataTable();
        var empty = table.ToList<UnsupportedMapping>();

        Assert.AreEqual(0, unsupported.Count);
        Assert.AreEqual(0, empty.Count);
    }

    private static SQLiteConnection OpenConnection()
    {
        var connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");
        connection.Open();
        return connection;
    }

    private class MappingEntity
    {
        [Column("DB_DISPLAY_NAME")]
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string MixedCase { get; set; }
        public string ReadOnlyValue { get; } = "只读初始值";
    }

    private sealed class ConflictingMappingEntity
    {
        [Column("Name")]
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string MixedCase { get; set; }
        public string ReadOnlyValue { get; } = "只读初始值";
    }

    private sealed class ConversionEntity
    {
        public MappingStatus Status { get; set; }
        public int? NullableNumber { get; set; }
        public Guid UniqueId { get; set; }
        public DateTime OccurredAt { get; set; }
        public string NullText { get; set; } = "默认文本";
    }

    private sealed class CombinedMappingEntity : MappingEntity
    {
        public MappingStatus Status { get; set; }
        public int? NullableNumber { get; set; }
        public Guid UniqueId { get; set; }
        public DateTime OccurredAt { get; set; }
        public string NullText { get; set; } = "默认文本";
    }

    private sealed class UnsupportedMapping
    {
        public UnsupportedMapping(string value)
        {
        }
    }

    private enum MappingStatus
    {
        Unknown,
        Started,
        Completed
    }
}
