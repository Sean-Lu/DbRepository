using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sean.Core.DbRepository.Test;

[TestClass]
public class WriteValuesBuilderTest
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SingleRow_UsesMappedValuesAndRetainsOriginalParameterObject(bool parameterized)
    {
        var row = new WriteRow { Name = "O'Brien", Count = 7 };
        var insert = InsertableSqlBuilder<WriteRow>.Create(DatabaseType.MySql)
            .InsertFields("total", "display_name").SetParameter(row).SetSqlIndented(false).SetSqlParameterized(parameterized).Build();
        var replace = ReplaceableSqlBuilder<WriteRow>.Create(DatabaseType.MySql)
            .InsertFields("total", "display_name").SetParameter(row).SetSqlIndented(false).SetSqlParameterized(parameterized).Build();

        foreach (var command in new[] { insert, replace })
        {
            var values = parameterized ? "(@Count, @Name)" : "(7, 'O''Brien')";
            StringAssert.EndsWith(command.Sql, "`WriteRow`(`total`, `display_name`) VALUES" + values);
            Assert.AreSame(row, command.Parameter);
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BulkRows_PreserveMappedFieldOrderNullsAndIndentedValues(bool parameterized)
    {
        var rows = new[] { new WriteRow { Name = "O'Brien", Count = 7 }, new WriteRow { Name = null, Count = 8 } };
        var insert = InsertableSqlBuilder<WriteRow>.Create(DatabaseType.MySql)
            .InsertFields("total", "display_name").SetParameter(rows).SetSqlIndented(true).SetSqlParameterized(parameterized).Build();
        var replace = ReplaceableSqlBuilder<WriteRow>.Create(DatabaseType.MySql)
            .InsertFields("total", "display_name").SetParameter(rows).SetSqlIndented(true).SetSqlParameterized(parameterized).Build();

        var values = parameterized ? "(@Count_1, @Name_1), " + Environment.NewLine + "(@Count_2, @Name_2)"
            : "(7, 'O''Brien'), " + Environment.NewLine + "(8, null)";
        foreach (var command in new[] { insert, replace })
        {
            StringAssert.EndsWith(command.Sql.Replace("\r\n", "\n"), ("`WriteRow`(`total`, `display_name`) \nVALUES" + values).Replace("\r\n", "\n"));
            if (parameterized)
            {
                var parameters = (IDictionary<string, object>)command.Parameter;
                CollectionAssert.AreEqual(new[] { "Count_1", "Name_1", "Count_2", "Name_2" }, parameters.Keys.ToArray());
                CollectionAssert.AreEqual(new object[] { 7, "O'Brien", 8, null }, parameters.Values.ToArray());
            }
        }
    }

    [TestMethod]
    public void UnmappedField_SingleRowAllowsExternalParameterButBulkRejectsMissingProperty()
    {
        var parameter = new { external = 3 };
        var insert = InsertableSqlBuilder<WriteRow>.Create(DatabaseType.MySql).InsertFields("external").SetParameter(parameter).SetSqlParameterized(true);
        var replace = ReplaceableSqlBuilder<WriteRow>.Create(DatabaseType.MySql).InsertFields("external").SetParameter(parameter).SetSqlParameterized(true);
        foreach (var command in new[] { insert.Build(), replace.Build() })
        {
            StringAssert.Contains(command.Sql, "VALUES(@external)");
            Assert.AreSame(parameter, command.Parameter);
        }

        // 单条可使用匿名参数，批量则必须从实体属性取值；不能在公共构建器中混淆这两种规则。
        var rows = new[] { new WriteRow() };
        var insertError = Assert.Throws<InvalidOperationException>(() => insert.SetParameter(rows).Build());
        var replaceError = Assert.Throws<InvalidOperationException>(() => replace.SetParameter(rows).Build());
        Assert.AreEqual(insertError.Message, replaceError.Message);
        StringAssert.Contains(insertError.Message, "field [external] not found");
    }

    private sealed class WriteRow
    {
        [Column("display_name")]
        public string Name { get; set; }
        [Column("total")]
        public int Count { get; set; }
    }
}
