using System.Reflection;
using MiniOrm.Attributes;

namespace MiniOrm.Data;
public class EntityMetadata
{
        public string TableName { get; init; } = string.Empty;

    public PropertyInfo PrimaryKeyProperty { get; init; } = null!;
    public List<ColumnMetadata> Columns { get; init; } = new();
}

public class ColumnMetadata
{
    public PropertyInfo Property { get; init; } = null!;
    public string ColumnName { get; init; } = string.Empty;
}
