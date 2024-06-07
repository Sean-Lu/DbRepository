using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Test
{
    [TestClass]
    public class NamingConventionTest
    {
        /// <summary>
        /// 帕斯卡命名法（Pascal Case）。示例：UserName、ProductPrice、IsValid
        /// </summary>
        [TestMethod]
        public void TestPascalCase()
        {
            Assert.AreEqual("CreateUserId", "CreateUserId".ToNamingConvention(NamingConvention.PascalCase));
            Assert.AreEqual("CreateUserId", "Create_User_Id".ToNamingConvention(NamingConvention.PascalCase));
            Assert.AreEqual("CreateUserId", "create_user_id".ToNamingConvention(NamingConvention.PascalCase));
            Assert.AreEqual("CreateUserId", "CREATE_USER_ID".ToNamingConvention(NamingConvention.PascalCase));

            Assert.AreEqual("CustomerAccount", "CustomerAccount".ToNamingConvention(NamingConvention.PascalCase));

            Assert.AreEqual("IpAddress", "IPAddress".ToNamingConvention(NamingConvention.PascalCase));
        }

        /// <summary>
        /// 驼峰命名法（Camel Case）。示例：userName、productPrice、isValid
        /// </summary>
        [TestMethod]
        public void TestCamelCase()
        {
            Assert.AreEqual("createUserId", "CreateUserId".ToNamingConvention(NamingConvention.CamelCase));
            Assert.AreEqual("createUserId", "Create_User_Id".ToNamingConvention(NamingConvention.CamelCase));
            Assert.AreEqual("createUserId", "create_user_id".ToNamingConvention(NamingConvention.CamelCase));
            Assert.AreEqual("createUserId", "CREATE_USER_ID".ToNamingConvention(NamingConvention.CamelCase));

            Assert.AreEqual("customerAccount", "CustomerAccount".ToNamingConvention(NamingConvention.CamelCase));

            Assert.AreEqual("ipAddress", "IPAddress".ToNamingConvention(NamingConvention.CamelCase));
        }

        /// <summary>
        /// 蛇形命名法（Snake Case）。示例：user_name、product_price、is_valid
        /// </summary>
        [TestMethod]
        public void TestSnakeCase()
        {
            Assert.AreEqual("create_user_id", "CreateUserId".ToNamingConvention(NamingConvention.SnakeCase));
            Assert.AreEqual("create_user_id", "Create_User_Id".ToNamingConvention(NamingConvention.SnakeCase));
            Assert.AreEqual("create_user_id", "create_user_id".ToNamingConvention(NamingConvention.SnakeCase));
            Assert.AreEqual("create_user_id", "CREATE_USER_ID".ToNamingConvention(NamingConvention.SnakeCase));

            Assert.AreEqual("customer_account", "CustomerAccount".ToNamingConvention(NamingConvention.SnakeCase));

            Assert.AreEqual("ip_address", "IPAddress".ToNamingConvention(NamingConvention.SnakeCase));
        }

        /// <summary>
        /// 大写蛇形命名法（Upper Snake Case）。示例：USER_NAME、PRODUCT_PRICE、IS_VALID
        /// </summary>
        [TestMethod]
        public void TestUpperSnakeCase()
        {
            Assert.AreEqual("CREATE_USER_ID", "CreateUserId".ToNamingConvention(NamingConvention.UpperSnakeCase));
            Assert.AreEqual("CREATE_USER_ID", "Create_User_Id".ToNamingConvention(NamingConvention.UpperSnakeCase));
            Assert.AreEqual("CREATE_USER_ID", "create_user_id".ToNamingConvention(NamingConvention.UpperSnakeCase));
            Assert.AreEqual("CREATE_USER_ID", "CREATE_USER_ID".ToNamingConvention(NamingConvention.UpperSnakeCase));

            Assert.AreEqual("CUSTOMER_ACCOUNT", "CustomerAccount".ToNamingConvention(NamingConvention.UpperSnakeCase));

            Assert.AreEqual("IP_ADDRESS", "IPAddress".ToNamingConvention(NamingConvention.UpperSnakeCase));
        }
    }
}
