using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AutoMapper;
using Example.Domain.Entities;

namespace Example.Application.Dtos
{
    /// <summary>
    /// 测试表（仅供测试使用）
    /// </summary>
    [AutoMap(typeof(TestEntity), ReverseMap = true)]
    public class TestDto
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        /// <summary>
        /// 用户id
        /// </summary>
        [Column("UserId")]
        public long UserId { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        [Column("UserName")]
        public string UserName { get; set; }
        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public SexType Sex { get; set; }
        /// <summary>
        /// 电话号码
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// 是否是VIP用户
        /// </summary>
        public bool IsVip { get; set; }
        /// <summary>
        /// 是否是黑名单用户
        /// </summary>
        public bool IsBlack { get; set; }
        /// <summary>
        /// 国家
        /// </summary>
        public CountryType Country { get; set; }
        /// <summary>
        /// 账户余额
        /// </summary>
        public decimal AccountBalance { get; set; }
        /// <summary>
        /// 账户余额
        /// </summary>
        public decimal AccountBalance2 { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}