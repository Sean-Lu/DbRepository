using ADOX;

namespace Example.Dapper.Infrastructure.Helper
{
    public static class MsAccessHelper
    {
        /// <summary>
        /// 创建 Access 数据库
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        public static void CreateDatabase(string connectionString)
        {
            // 需要在项目里面添加以下2个COM引用（ADODB、ADOX）：
            // 1. Microsoft ActiveX Data Objects 2.8 Library
            // 2. Microsoft ADO Ext. 2.8 for DDL and Security

            Catalog catalog = new Catalog();
            catalog.Create(connectionString);
        }
    }
}
