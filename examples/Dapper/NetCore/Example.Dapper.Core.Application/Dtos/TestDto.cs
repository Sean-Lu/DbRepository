using System;
using AutoMapper;
using Example.Dapper.Core.Domain.Entities;

namespace Example.Dapper.Core.Application.Dtos
{
    /// <summary>
    /// 测试表
    /// </summary>
    [AutoMap(typeof(TestEntity), ReverseMap = true)]
    public class TestDto
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 用户id
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
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
        /// 是否VIP用户
        /// </summary>
        public bool IsVip { get; set; }
        /// <summary>
        /// 是否黑名单用户
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