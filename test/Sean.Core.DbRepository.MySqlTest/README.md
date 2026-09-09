# MySQL 集成测试

默认跳过，不连接数据库。确认测试服务器可用后，通过环境变量 `SEAN_MYSQL_CONNECTION_STRING` 提供连接字符串（勿提交凭据），在仓库根目录运行：

```powershell
$env:SEAN_MYSQL_TESTS = '1'
try {
    dotnet test test/Sean.Core.DbRepository.MySqlTest/Sean.Core.DbRepository.MySqlTest.csproj -c Release
} finally {
    Remove-Item Env:SEAN_MYSQL_TESTS -ErrorAction SilentlyContinue
    Remove-Item Env:SEAN_MYSQL_CONNECTION_STRING -ErrorAction SilentlyContinue
}
```

仅使用测试服务器，账号需有建库、删库及建表权限。测试忽略连接字符串中的 Database，创建并清理独立的 `sean_orm_g4_<GUID>` 库，不修改已有库。异常中断可能留下临时库，核实后再手动清理。
