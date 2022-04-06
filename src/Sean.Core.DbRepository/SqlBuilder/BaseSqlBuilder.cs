namespace Sean.Core.DbRepository
{
    public abstract class BaseSqlBuilder
    {
        public ISqlAdapter SqlAdapter { get; set; }
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }
    }
}