using System.Reflection;
using MiniOrm.Attributes;

namespace MiniOrm.Data;
public static class TypeMapper
{
    public static EntityMetadata BuildMetadata<T>()
    {
        Type type = typeof(T);
        //table name
        var tableAttr = type.GetCustomAttribute<TableAttribute>()
            ?? throw new InvalidOperationException(
                $"Entity '{type.Name}' is missing a [Table] attribute.");

        //primary key
        var pkProperty = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
            ?? throw new InvalidOperationException(
                $"Entity '{type.Name}' has no property with [PrimaryKey].");

        //columns
        var columns = type.GetProperties()
            .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null)
            .Select(p => new ColumnMetadata
            {
                Property   = p,
                ColumnName = p.GetCustomAttribute<ColumnAttribute>()!.Name
            })
            .ToList();

        return new EntityMetadata
        {
            TableName          = tableAttr.Name,
            PrimaryKeyProperty = pkProperty,
            Columns            = columns
        };
    }

    public static string GetPostgresColumnDefinition(PropertyInfo property, bool isPrimaryKey)
    {
        if (isPrimaryKey)
            return "SERIAL PRIMARY KEY";

        Type propType = property.PropertyType;

        //detect nullable
        Type? underlyingType = Nullable.GetUnderlyingType(propType);
        
        bool isNullable;
        if (underlyingType != null)
        {
            isNullable = true;
        }
        else
        {
            isNullable = false;
        }

        Type effectiveType;
        if (underlyingType != null)
        {
            effectiveType = underlyingType;
        }
        else
        {
            effectiveType = propType;
        }
        //for string nullability
        if (effectiveType == typeof(string))
        {
            var ctx  = new NullabilityInfoContext();
            var info = ctx.Create(property);
            bool stringIsNullable = info.WriteState == NullabilityState.Nullable;
            if (stringIsNullable)
            {
                return "TEXT NULL";
            }
            else
            {
                return "TEXT NOT NULL";
            }
        }

        string pgType = effectiveType switch
        {
            _ when effectiveType == typeof(int)      => "INTEGER",
            _ when effectiveType == typeof(long)     => "BIGINT",
            _ when effectiveType == typeof(float)    => "REAL",
            _ when effectiveType == typeof(double)   => "DOUBLE PRECISION",
            _ when effectiveType == typeof(decimal)  => "NUMERIC",
            _ when effectiveType == typeof(bool)     => "BOOLEAN",
            _ when effectiveType == typeof(DateTime) => "TIMESTAMP",
            _ when effectiveType == typeof(Guid)     => "UUID",
            _ => throw new NotSupportedException(
                $"No Postgres mapping for C# type '{effectiveType.FullName}'.")
        };

        // append NULL/NOT NULL
        string nullability;
        if (isNullable)
        {
            nullability = "NULL";
        }
        else
        {
            nullability = "NOT NULL";
        }

        return $"{pgType} {nullability}";
    }
}
