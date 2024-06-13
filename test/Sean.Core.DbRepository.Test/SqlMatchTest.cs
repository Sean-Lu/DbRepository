using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Test
{
    [TestClass]
    public class SqlMatchTest
    {
        [TestMethod]
        public void TestMatchSqlOperation()
        {
            Assert.IsFalse(SqlMatchUtil.IsWriteOperation("SELECT * FROM table"));

            Assert.IsTrue(SqlMatchUtil.IsWriteOperation("insert INTO table (column) VALUES (value)"));
            Assert.IsTrue(SqlMatchUtil.IsWriteOperation("Update table SET column = value WHERE condition"));
            Assert.IsTrue(SqlMatchUtil.IsWriteOperation("DELETE FROM table WHERE condition"));
            Assert.IsTrue(SqlMatchUtil.IsWriteOperation("   replace INTO table (column) VALUES (value)"));
            Assert.IsTrue(SqlMatchUtil.IsWriteOperation("ALTER TABLE table ADD column datatype"));
        }
    }
}
