namespace MiniOrm.Attributes;
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class PrimaryKeyAttribute : Attribute
{   
     public const string ColumnName = "id";
}
