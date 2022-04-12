﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sean.Core.DbRepository
{
    public class TableFieldInfoForSqlBuilder
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 是否是主键字段 <see cref="KeyAttribute"/>
        /// </summary>
        public bool PrimaryKey { get; set; }
        /// <summary>
        /// 是否是自增字段  <see cref="DatabaseGeneratedOption.Identity"/>
        /// </summary>
        public bool Identity { get; set; }

        public Func<string, ISqlAdapter, string> SetFieldCustomHandler { get; set; }
    }
}
