using System;

namespace Sean.Core.DbRepository;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IndexAttribute : Attribute
{
    public string[] IndexPropertyNames { get; set; }
    public string IndexName { get; set; }
    public DbIndexType IndexType { get; set; }

    public IndexAttribute(string indexPropertyName) : this(indexPropertyName, null, DbIndexType.Normal)
    {
    }
    public IndexAttribute(string indexPropertyName, string indexName) : this(indexPropertyName, indexName, DbIndexType.Normal)
    {
    }
    public IndexAttribute(string indexPropertyName, string indexName, DbIndexType indexType) : this(new[] { indexPropertyName }, indexName, indexType)
    {
    }

    public IndexAttribute(string[] indexPropertyNames) : this(indexPropertyNames, null, DbIndexType.Normal)
    {
    }
    public IndexAttribute(string[] indexPropertyNames, string indexName) : this(indexPropertyNames, indexName, DbIndexType.Normal)
    {
    }
    public IndexAttribute(string[] indexPropertyNames, string indexName, DbIndexType indexType)
    {
        IndexPropertyNames = indexPropertyNames ?? throw new ArgumentNullException(nameof(indexPropertyNames));
        IndexName = indexName;
        IndexType = indexType;
    }
}