using System;

namespace Sean.Core.DbRepository;

public class SynchronousWriteOptions
{
    /// <summary>
    /// Enable synchronous write mode. It is commonly used in SQLite database to solve database locked issue caused by concurrent writes.
    /// <para>启用同步写入模式。通常用于SQLite数据库来解决并发写入导致的锁库问题。</para>
    /// </summary>
    public bool Enable { get; set; } = false;
    /// <summary>
    /// 同步写入锁等待超时时间（单位：毫秒），默认值：5000
    /// </summary>
    public int LockTimeout { get; set; } = 5000;
    /// <summary>
    /// <para>参数：sql</para>
    /// <para>返回值：是否继续执行SQL</para>
    /// </summary>
    public Func<string, bool> OnLockTimeout { get; set; }
}