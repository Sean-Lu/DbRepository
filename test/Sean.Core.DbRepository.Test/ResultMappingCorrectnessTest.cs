using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

/// <summary>
/// 同一结果集在 DataRow、DataTable、同步及异步 Reader 上必须使用一致的映射规则。
/// </summary>
[TestClass]
public class ResultMappingCorrectnessTest
{
    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task NullableScalars_ConvertValuesAndPreserveDbNull(string source)
    {
        var id = Guid.NewGuid();
        var date = new DateTime(2026, 9, 6, 12, 34, 56);
        await AssertValues<int?>(source, new object[] { 42L, DBNull.Value }, 42, null);
        await AssertValues<decimal?>(source, new object[] { 12.5m, DBNull.Value }, 12.5m, null);
        await AssertValues<DateTime?>(source, new object[] { "2026-09-06 12:34:56", DBNull.Value }, date, null);
        await AssertValues<MappingState?>(source, new object[] { 2L, DBNull.Value }, MappingState.Done, null);
        await AssertValues<Guid?>(source, new object[] { id, DBNull.Value }, id, null);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task EmptyDateStrings_PreserveNullableAndNonNullableDefaults(string source)
    {
        // 原转换器将空字符串日期映射为目标类型的默认值，不能把 DateTime? 的 null 改成 MinValue。
        await AssertValues<DateTime?>(source, new object[] { string.Empty }, new DateTime?[] { null });
        await AssertValues<DateTime>(source, new object[] { string.Empty }, DateTime.MinValue);
        using var table = CreateTable(new[] { "Value" }, new object[] { string.Empty });
        Assert.IsNull((await Map<DateEntity>(table, source))[0].Value);
        Assert.IsNull((await Map<GenericDto<DateTime?>>(table, source))[0].Value);
        Assert.IsNull((await Map<Tuple<DateTime?>>(table, source))[0].Item1);
        Assert.IsNull((await Map<ValueTuple<DateTime?>>(table, source))[0].Item1);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task DbNullScalar_DoesNotInvokeValueTypeConstructor(string source)
    {
        Assert.AreEqual(23, new ConstructedScalar().Value);
        using var table = CreateTable(new[] { "Value" }, new object[] { DBNull.Value });
        // 原标量分支返回 default(T)，不能改为 Activator 调用结构体的显式无参构造函数。
        Assert.AreEqual(0, (await Map<ConstructedScalar>(table, source))[0].Value);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task DbNullTupleItem_PreservesExplicitStructConstructorBehavior(string source)
    {
        // 记录少见但已有的差异：DataRow 用 null 调用元组构造器，Reader 用 Activator 取元素默认值。
        var originalRow = (Tuple<ConstructedScalar>)Activator.CreateInstance(typeof(Tuple<ConstructedScalar>), new object[] { null });
        var originalReader = Activator.CreateInstance<ConstructedScalar>();
        Assert.AreEqual(0, originalRow.Item1.Value);
        Assert.AreEqual(23, originalReader.Value);
        var expected = source.StartsWith("Reader", StringComparison.Ordinal) ? 23 : 0;
        using var table = CreateTable(new[] { "Value" }, new object[] { DBNull.Value });
        Assert.AreEqual(expected, (await Map<Tuple<ConstructedScalar>>(table, source))[0].Item1.Value);
        Assert.AreEqual(expected, (await Map<ValueTuple<ConstructedScalar>>(table, source))[0].Item1.Value);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task GenericDtos_UseEntityMappingAndPreserveInitializedProperties(string source)
    {
        using var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("vAlUe", typeof(long));
        table.Columns.Add("ReadOnly", typeof(string));
        table.Columns.Add("Unknown", typeof(string));
        table.Rows.Add("数据库列", 2L, "不得覆盖", "忽略");
        table.Rows.Add(DBNull.Value, DBNull.Value, "不得覆盖", "忽略");

        var entities = await Map<GenericDto<MappingState?>>(table, source);
        Assert.AreEqual(ExpectedCount(source, 2), entities.Count);
        Assert.IsNotNull(entities[0]);
        Assert.AreEqual("数据库列", entities[0].DisplayName);
        Assert.AreEqual("属性名初始值", entities[0].Name);
        Assert.AreEqual(MappingState.Done, entities[0].Value);
        Assert.AreEqual("只读初始值", entities[0].ReadOnly);
        if (entities.Count == 2)
        {
            Assert.AreEqual("显示初始值", entities[1].DisplayName);
            Assert.IsNull(entities[1].Value);
        }

        // 声明 Column 后仍允许查询按属性名提供别名，不能改变原有回退规则。
        using var aliases = new DataTable();
        aliases.Columns.Add("displayname", typeof(string));
        aliases.Rows.Add("属性名别名");
        Assert.AreEqual("属性名别名", (await Map<GenericDto<int>>(aliases, source))[0].DisplayName);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task TupleNamedDtos_AreNotMistakenForSystemTuples(string source)
    {
        using var table = CreateTable(new[] { "Item1" }, new object[] { 23L });
        Assert.AreEqual(23, (await Map<TupleDto<int>>(table, source))[0].Item1);
        Assert.AreEqual(23, (await Map<ValueTupleDto<int>>(table, source))[0].Item1);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task OrdinaryEntities_ConvertNullablePropertiesWithoutOverwritingDbNullDefaults(string source)
    {
        var id = Guid.NewGuid();
        using var table = CreateTable(new[] { "State", "Id", "Number", "Text" },
            new object[] { 2L, id, 42L, "非空" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value });
        var entities = await Map<NullableEntity>(table, source);
        Assert.AreEqual(MappingState.Done, entities[0].State);
        Assert.AreEqual(id, entities[0].Id);
        Assert.AreEqual(42, entities[0].Number);
        Assert.AreEqual("非空", entities[0].Text);
        if (entities.Count == 2)
        {
            Assert.AreEqual(MappingState.Ready, entities[1].State);
            Assert.AreEqual(Guid.Empty, entities[1].Id);
            Assert.AreEqual(17, entities[1].Number);
            Assert.AreEqual("初始值", entities[1].Text);
        }
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task Tuples_ConvertEveryItemAndPreserveDbNullDefaults(string source)
    {
        var id = Guid.NewGuid();
        using var table = CreateTable(new[] { "a", "b", "c", "d", "e", "f" },
            new object[] { 7L, 2L, id, "2026-09-06 12:34:56", 12.5m, "文本" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value });
        var expected = (7, (MappingState?)MappingState.Done, (Guid?)id,
            (DateTime?)new DateTime(2026, 9, 6, 12, 34, 56), (decimal?)12.5m, "文本");
        var values = await Map<(int, MappingState?, Guid?, DateTime?, decimal?, string)>(table, source);
        Assert.AreEqual(expected, values[0]);
        var tuples = await Map<Tuple<int, MappingState?, Guid?, DateTime?, decimal?, string>>(table, source);
        Assert.AreEqual(Tuple.Create(expected.Item1, expected.Item2, expected.Item3,
            expected.Item4, expected.Item5, expected.Item6), tuples[0]);
        if (values.Count == 2)
        {
            Assert.AreEqual(default, values[1]);
            Assert.AreEqual(Tuple.Create(0, (MappingState?)null, (Guid?)null, (DateTime?)null,
                (decimal?)null, (string)null), tuples[1]);
        }
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task Tuples_MissingColumnsUseDefaultsAndExtraColumnsAreIgnored(string source)
    {
        using var shortTable = CreateTable(new[] { "a" }, new object[] { 9L });
        Assert.AreEqual((9, (string)null, (int?)null),
            (await Map<(int, string, int?)>(shortTable, source))[0]);
        Assert.AreEqual(Tuple.Create(9, (string)null, (int?)null),
            (await Map<Tuple<int, string, int?>>(shortTable, source))[0]);
        using var extraTable = CreateTable(new[] { "a", "b", "c" }, new object[] { 11L, "保留", "忽略" });
        Assert.AreEqual((11, "保留"), (await Map<(int, string)>(extraTable, source))[0]);
        Assert.AreEqual(Tuple.Create(11, "保留"), (await Map<Tuple<int, string>>(extraTable, source))[0]);
        using var nestedTable = CreateTable(new[] { "a", "b" }, new object[] { Tuple.Create(13), (17, "嵌套") });
        Assert.AreEqual((Tuple.Create(13), (17, "嵌套")),
            (await Map<(Tuple<int>, (int, string))>(nestedTable, source))[0]);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task Tuples_PreserveAssignableReferenceValues(string source)
    {
        var values = new List<int> { 1, 2, 3 };
        var original = (Tuple<IEnumerable<int>, IComparable>)Activator.CreateInstance(
            typeof(Tuple<IEnumerable<int>, IComparable>), new object[] { values, "字符串" });
        Assert.AreSame(values, original.Item1);
        using var table = CreateTable(new[] { "a", "b" }, new object[] { values, "字符串" });
        // DataRow 原构造调用已支持接口和基类入参，统一转换后不能将合法赋值变成异常。
        var tuple = (await Map<Tuple<IEnumerable<int>, IComparable>>(table, source))[0];
        Assert.AreSame(values, tuple.Item1);
        Assert.AreEqual("字符串", tuple.Item2);
        var valueTuple = (await Map<(IEnumerable<int>, IComparable)>(table, source))[0];
        Assert.AreSame(values, valueTuple.Item1);
        Assert.AreEqual("字符串", valueTuple.Item2);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task LongTuples_MapRestFromRemainingColumnsAndPreserveBoxedRest(string source)
    {
        using var table = CreateTable(Enumerable.Range(1, 15).Select(i => "c" + i).ToArray(),
            Enumerable.Range(1, 15).Select(i => (object)(long)i).ToArray());
        Assert.AreEqual((1, 2, 3, 4, 5, 6, 7, 8),
            (await Map<(int, int, int, int, int, int, int, int)>(table, source))[0]);
        var longValue = (await Map<(int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)>(table, source))[0];
        Assert.AreEqual((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15), longValue);
        Assert.AreEqual(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8),
            (await Map<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(table, source))[0]);

        // 原先已可传入装箱后的 Rest 元组，该形式不能被误拆成普通列值。
        using var boxed = CreateTable(Enumerable.Range(1, 8).Select(i => "c" + i).ToArray(),
            new object[] { 1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9) });
        var reference = (await Map<Tuple<int, int, int, int, int, int, int, Tuple<int, int>>>(boxed, source))[0];
        Assert.AreEqual(Tuple.Create(8, 9), reference.Rest);
        boxed.Rows[0][7] = (8, 9);
        Assert.AreEqual((1, 2, 3, 4, 5, 6, 7, 8, 9),
            (await Map<(int, int, int, int, int, int, int, int, int)>(boxed, source))[0]);

        using var missing = CreateTable(new[] { "a" }, new object[] { 1L });
        Assert.AreEqual((1, 0, 0, 0, 0, 0, 0, 0),
            (await Map<(int, int, int, int, int, int, int, int)>(missing, source))[0]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LongTuples_ReadEachRequiredColumnOnceInOrder(bool asynchronous)
    {
        using var reader = new SequentialReader(Enumerable.Range(1, 15).Select(i => (object)(long)i).ToArray());
        var values = asynchronous
            ? await reader.GetListAsync<(int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)>()
            : reader.GetList<(int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)>();
        Assert.AreEqual((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15), values.Single());
        Assert.AreEqual(15, reader.ReadValues);
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task EntityMapping_DoesNotWriteStaticOrIndexedProperties(string source)
    {
        using var table = CreateTable(new[] { "GlobalValue", "Item", "Value", "PrivateValue" },
            new object[] { "数据库值", "不能写入索引器", 23L, 29L });
        StaticEntity.GlobalValue = "全局配置";
        try
        {
            var entity = (await Map<StaticEntity>(table, source))[0];
            Assert.AreEqual("全局配置", StaticEntity.GlobalValue);
            Assert.AreEqual(23, entity.Value);
            Assert.AreEqual(29, entity.PrivateValue, "保留原先可映射私有 setter 的行为。");
        }
        finally
        {
            StaticEntity.GlobalValue = "全局配置";
        }
    }

    [TestMethod]
    [DataRow("Row")]
    [DataRow("Table")]
    [DataRow("TableFirst")]
    [DataRow("Reader")]
    [DataRow("ReaderAsync")]
    [DataRow("ReaderFirst")]
    [DataRow("ReaderFirstAsync")]
    public async Task InvalidConversions_ThrowInsteadOfReturningDefault(string source)
    {
        using var overflow = CreateTable(new[] { "Value" }, new object[] { 2147483648L });
        await Assert.ThrowsAsync<OverflowException>(() => Map<int?>(overflow, source));
        await Assert.ThrowsAsync<OverflowException>(() => Map<GenericDto<int>>(overflow, source));
        await Assert.ThrowsAsync<OverflowException>(() => Map<Tuple<int>>(overflow, source));
        using var invalid = CreateTable(new[] { "Value" }, new object[] { "不是数字" });
        await Assert.ThrowsAsync<FormatException>(() => Map<int?>(invalid, source));
        await Assert.ThrowsAsync<FormatException>(() => Map<ValueTuple<int>>(invalid, source));
        await Assert.ThrowsAsync<NotSupportedException>(() => Map<NoDefaultConstructor<int>>(invalid, source));
    }

    [TestMethod]
    public async Task EmptyAndNullInputs_PreserveOriginalResults()
    {
        DataRow row = null;
        DataTable table = null;
        System.Data.Common.DbDataReader reader = null;
        Assert.IsNull(row.ToEntity<int?>());
        Assert.IsNull(table.ToEntity<GenericDto<int>>());
        Assert.IsNull(table.ToList<int?>());
        Assert.IsNull(reader.Get<int?>());
        Assert.IsNull(reader.GetList<int?>());
        Assert.IsNull(await reader.GetAsync<int?>());
        Assert.IsNull(await reader.GetListAsync<int?>());
        using var empty = new DataTable();
        empty.Columns.Add("Value", typeof(long));
        Assert.IsNull(empty.ToEntity<NoDefaultConstructor<int>>());
        Assert.AreEqual(0, empty.ToList<NoDefaultConstructor<int>>().Count);
        using var sync = empty.CreateDataReader();
        Assert.AreEqual(0, sync.GetList<NoDefaultConstructor<int>>().Count);
        using var asyncReader = empty.CreateDataReader();
        Assert.AreEqual(0, (await asyncReader.GetListAsync<NoDefaultConstructor<int>>()).Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SQLite_RealReaderAndLoadedTableAgree(bool asynchronous)
    {
        using var connection = new SQLiteConnection("Data Source=:memory:;Version=3;Pooling=False;");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 7 AS Value, 2 AS State, '数据库列' AS Name UNION ALL SELECT NULL, NULL, NULL";
        using var table = new DataTable();
        using (var reader = asynchronous ? await command.ExecuteReaderAsync() : command.ExecuteReader())
        {
            table.Load(reader);
        }
        using (var reader = asynchronous ? await command.ExecuteReaderAsync() : command.ExecuteReader())
        {
            var values = asynchronous ? await reader.GetListAsync<int?>() : reader.GetList<int?>();
            CollectionAssert.AreEqual(new int?[] { 7, null }, values);
            CollectionAssert.AreEqual(values, table.ToList<int?>());
        }
        using (var reader = asynchronous ? await command.ExecuteReaderAsync() : command.ExecuteReader())
        {
            var values = asynchronous ? await reader.GetListAsync<(int?, MappingState?, string)>()
                : reader.GetList<(int?, MappingState?, string)>();
            Assert.AreEqual(((int?)7, (MappingState?)MappingState.Done, "数据库列"), values[0]);
            Assert.AreEqual(default, values[1]);
            CollectionAssert.AreEqual(values, table.ToList<(int?, MappingState?, string)>());
        }
        using (var reader = asynchronous ? await command.ExecuteReaderAsync() : command.ExecuteReader())
        {
            var values = asynchronous ? await reader.GetListAsync<GenericDto<int?>>() : reader.GetList<GenericDto<int?>>();
            Assert.AreEqual(7, values[0].Value);
            Assert.AreEqual("数据库列", values[0].DisplayName);
            Assert.IsNull(values[1].Value);
            Assert.AreEqual("显示初始值", values[1].DisplayName);
        }
    }

    private static async Task AssertValues<T>(string source, object[] values, params T[] expected)
    {
        using var table = CreateTable(new[] { "Value" }, values.Select(value => new[] { value }).ToArray());
        var actual = await Map<T>(table, source);
        CollectionAssert.AreEqual(expected.Take(ExpectedCount(source, expected.Length)).ToArray(), actual);
    }

    private static int ExpectedCount(string source, int count) => source.Contains("First") ? Math.Min(1, count) : count;

    private static async Task<List<T>> Map<T>(DataTable table, string source)
    {
        switch (source)
        {
            case "Row": return table.Rows.Cast<DataRow>().Select(row => row.ToEntity<T>()).ToList();
            case "Table": return table.ToList<T>();
            case "TableFirst": return new List<T> { table.ToEntity<T>() };
            default:
                using (var reader = table.CreateDataReader())
                {
                    switch (source)
                    {
                        case "Reader": return reader.GetList<T>();
                        case "ReaderAsync": return await reader.GetListAsync<T>();
                        case "ReaderFirst": return new List<T> { reader.Get<T>() };
                        case "ReaderFirstAsync": return new List<T> { await reader.GetAsync<T>() };
                        default: throw new ArgumentOutOfRangeException(nameof(source));
                    }
                }
        }
    }

    private static DataTable CreateTable(string[] names, params object[][] rows)
    {
        var table = new DataTable();
        foreach (var name in names)
        {
            // 保留数据库提供的运行时值类型，不能由 DataColumn 预先帮映射器完成转换。
            table.Columns.Add(name, typeof(object));
        }
        foreach (var row in rows)
        {
            table.Rows.Add(row);
        }
        return table;
    }

    private enum MappingState { Ready = 1, Done = 2 }

    private class GenericDto<T>
    {
        [Column("Name")]
        public string DisplayName { get; set; } = "显示初始值";
        public string Name { get; set; } = "属性名初始值";
        public T Value { get; set; }
        public string ReadOnly { get; } = "只读初始值";
    }

    private sealed class TupleDto<T> { public T Item1 { get; set; } }
    private sealed class ValueTupleDto<T> { public T Item1 { get; set; } }
    private sealed class NoDefaultConstructor<T> { public NoDefaultConstructor(T value) { } }
    private sealed class DateEntity { public DateTime? Value { get; set; } = new DateTime(2000, 1, 1); }
    private struct ConstructedScalar
    {
        public ConstructedScalar() => Value = 23;
        public int Value { get; }
    }

    private sealed class NullableEntity
    {
        public MappingState? State { get; set; } = MappingState.Ready;
        public Guid? Id { get; set; } = Guid.Empty;
        public int? Number { get; set; } = 17;
        public string Text { get; set; } = "初始值";
    }

    private sealed class StaticEntity
    {
        public static string GlobalValue { get; set; } = "全局配置";
        public int Value { get; set; }
        public int PrivateValue { get; private set; }
        public string this[int index]
        {
            get => throw new InvalidOperationException("不能访问索引器");
            set => throw new InvalidOperationException("不能访问索引器");
        }
    }

    /// <summary>
    /// 模拟顺序读取限制：每个有效列只能读取一次，末尾另有一个不应被映射器触碰的列。
    /// </summary>
    private sealed class SequentialReader : DbDataReader
    {
        private readonly object[] _values;
        private bool _read;
        private bool _closed;
        public int ReadValues { get; private set; }
        public SequentialReader(object[] values) => _values = values;
        public override object GetValue(int ordinal)
        {
            if (ordinal != ReadValues || ordinal >= _values.Length)
            {
                throw new InvalidOperationException("重复读取、回退读取或触碰了多余列。");
            }
            ReadValues++;
            return _values[ordinal];
        }
        public override bool Read()
        {
            if (_read) return false;
            _read = true;
            return true;
        }
        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++) values[i] = GetValue(i);
            return count;
        }
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => throw new NotSupportedException();
        public override int FieldCount => _values.Length + 1;
        public override bool HasRows => true;
        public override bool IsClosed => _closed;
        public override int Depth => 0;
        public override int RecordsAffected => -1;
        public override void Close() => _closed = true;
        public override bool NextResult() => false;
        public override string GetName(int ordinal) => "c" + ordinal;
        public override Type GetFieldType(int ordinal) => typeof(object);
        public override string GetDataTypeName(int ordinal) => "object";
        public override int GetOrdinal(string name) => throw new NotSupportedException();
        public override bool IsDBNull(int ordinal) => GetValue(ordinal) == DBNull.Value;
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
        public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
        public override byte GetByte(int ordinal) => throw new NotSupportedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override char GetChar(int ordinal) => throw new NotSupportedException();
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
        public override short GetInt16(int ordinal) => throw new NotSupportedException();
        public override int GetInt32(int ordinal) => throw new NotSupportedException();
        public override long GetInt64(int ordinal) => throw new NotSupportedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
        public override string GetString(int ordinal) => throw new NotSupportedException();
        public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
        public override double GetDouble(int ordinal) => throw new NotSupportedException();
        public override float GetFloat(int ordinal) => throw new NotSupportedException();
    }
}
