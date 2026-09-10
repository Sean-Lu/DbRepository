using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class SqlParameterFormattingTest
{
    [TestMethod]
    [DataRow("en-US")]
    [DataRow("fr-FR")]
    [DataRow("tr-TR")]
    public void Formatting_PreservesExistingNamesAndDialectPrefixes(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var names = new[]
            {
                "A", "Z", "a", "z", "Id_1000", "Name`", "A[", "@Id", ":Id", "$Id",
                "_Id", "0Id", "中文", "[Id]", "[[Id[[", "`Id`", "`", "[",
                "\u00AD`Id`", "\0`Id`", "\u200B`Id`"
            };
            foreach (var database in new[] { DatabaseType.MySql, DatabaseType.Oracle, DatabaseType.Dameng, DatabaseType.DuckDB })
            foreach (var name in names)
            {
                // 特殊字符在不同运行时的文化比较可能不同；以优化前的处理规则保护兼容性。
                var originalName = name.StartsWith("[") ? name.Trim('[')
                    : name.StartsWith("`") ? name.Trim('`') : name;
                var prefix = database == DatabaseType.Oracle || database == DatabaseType.Dameng ? ":"
                    : database == DatabaseType.DuckDB ? "$" : "@";
                Assert.AreEqual(prefix + originalName, database.MarkAsSqlParameter(name), database.ToString());
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [TestMethod]
    public void Formatting_RejectsEmptyNamesBeforeReadingFirstCharacter()
    {
        foreach (var name in new[] { null, "", " ", "\t\r\n" })
        {
            var error = Assert.Throws<ArgumentException>(() => DatabaseType.MySql.MarkAsSqlParameter(name));
            Assert.AreEqual("parameter", error.ParamName);
        }
    }

    [TestMethod]
    public void BulkInsert_PreservesSqlParameterOrderAndValues()
    {
        var builder = InsertableSqlBuilder<BulkParameterRow>.Create(DatabaseType.MySql)
            .InsertFields(row => new { row.Id, row.Name })
            .SetParameter(new[] { new BulkParameterRow { Id = 7, Name = "O'Brien" }, new BulkParameterRow { Id = 8, Name = "keep" } })
            .SetSqlParameterized(true).SetSqlIndented(false);
        // 精确保护调用方可见的 SQL 和参数顺序；重复 Build 不能改变参数名或值。
        for (var i = 0; i < 2; i++)
        {
            var command = builder.Build();
            Assert.AreEqual("INSERT INTO `BulkParameterRow`(`Id`, `Name`) VALUES(@Id_1, @Name_1), (@Id_2, @Name_2)", command.Sql);
            var parameters = (IDictionary<string, object>)command.Parameter;
            CollectionAssert.AreEqual(new[] { "Id_1", "Name_1", "Id_2", "Name_2" }, parameters.Keys.ToArray());
            CollectionAssert.AreEqual(new object[] { 7, "O'Brien", 8, "keep" }, parameters.Values.ToArray());
        }
    }

    private sealed class BulkParameterRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
