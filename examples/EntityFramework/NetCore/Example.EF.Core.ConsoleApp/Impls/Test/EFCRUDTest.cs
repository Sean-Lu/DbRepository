using Example.EF.Core.ConsoleApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace Example.EF.Core.ConsoleApp.Impls.Test
{
    /// <summary>
    /// EF 增\删\改\查
    /// </summary>
    internal class EFCRUDTest
    {
        #region 增加
        public void AddMethod1()
        {
            using (var db = new EFDbContext())
            {
                var entity = new TestEntity
                {
                    UserId = 10001,
                    UserName = "Test01"
                };
                //db.TestEntities.Add(entity);
                db.Add(entity);
                var success = db.SaveChanges() > 0;
            }
        }
        public void AddMethod2()
        {
            using (var db = new EFDbContext())
            {
                var entity = new TestEntity
                {
                    UserId = 10001,
                    UserName = "Test01"
                };
                db.Entry(entity).State = EntityState.Added;
                var success = db.SaveChanges() > 0;
            }
        }
        public void AddMethod3()
        {
            using (var db = new EFDbContext())
            {
                var sql = "INSERT INTO Test(UserId, UserName) VALUES(10001, 'Test01');";
                var success = db.Database.ExecuteSqlRaw(sql) > 0;
            }
        }
        #endregion

        #region 删除
        public void DeleteMethod1()
        {
            using (var db = new EFDbContext())
            {
                //var entity = db.TestEntities.Find(1000);
                var entity = db.Find<TestEntity>(1000);
                if (entity != null)
                {
                    //db.TestEntities.Remove(entity);
                    db.Remove(entity);
                    var success = db.SaveChanges() > 0;
                }
            }
        }
        public void DeleteMethod2()
        {
            using (var db = new EFDbContext())
            {
                var entity = new TestEntity
                {
                    Id = 1000
                };
                db.Entry(entity).State = EntityState.Deleted;
                var success = db.SaveChanges() > 0;
            }
        }
        public void DeleteMethod3()
        {
            using (var db = new EFDbContext())
            {
                var sql = "DELETE FROM Test WHERE Id=1000;";
                var success = db.Database.ExecuteSqlRaw(sql) > 0;
            }
        }
        #endregion

        #region 修改
        public void UpdateMethod1()
        {
            using (var db = new EFDbContext())
            {
                //var entity = db.TestEntities.Find(1000);
                var entity = db.Find<TestEntity>(1000);
                if (entity != null)
                {
                    entity.UserName = "Test02";
                    entity.Remark = "Test";
                    var success = db.SaveChanges() > 0;
                }
            }
        }
        public void UpdateMethod2()
        {
            using (var db = new EFDbContext())
            {
                var entity = new TestEntity
                {
                    Id = 1000,
                    UserName = "Test02",
                    Remark = "Test"
                };
                db.Entry(entity).State = EntityState.Modified;
                var success = db.SaveChanges() > 0;
            }
        }
        public void UpdateMethod3()
        {
            using (var db = new EFDbContext())
            {
                var sql = "UPDATE Test SET UserName='Test02', Remark='Test' WHERE Id=1000;";
                var success = db.Database.ExecuteSqlRaw(sql) > 0;
            }
        }
        #endregion

        #region 查询
        public void QueryMethod1()
        {
            using (var db = new EFDbContext())
            {
                //var result = db.TestEntities.FirstOrDefault(c => c.UserId == 100001);
                var result = db.Set<TestEntity>().FirstOrDefault(c => c.UserId == 10001);
            }
        }
        public void QueryMethod2()
        {
            using (var db = new EFDbContext())
            {
                //var result = db.TestEntities.Find(1000);
                var result = db.Set<TestEntity>().Find(1000);
            }
        }
        public void QueryMethod3()
        {
            using (var db = new EFDbContext())
            {
                var sql = "SELECT * FROM Test WHERE Id=1000;";
                //var result = db.Database.SqlQuery<TestEntity>(sql);
            }
        }
        #endregion
    }
}
