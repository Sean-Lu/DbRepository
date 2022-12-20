using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example.NetFramework.Entities
{
    /// <summary>
    /// 测试表
    /// </summary>
    [Table("Test", Schema = "test")]
    public class TestEntity
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual long Id { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; set; }
    }
}