using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MiniOrm.Attributes;
using Npgsql;

namespace MiniOrm.Data;

public class DbSet<T> where T : class, new()
{
    private readonly DbContext _dbContext;
    private readonly EntityMetadata _metadata;

    public DbSet(DbContext dbContext, EntityMetadata metadata)
    {
        _dbContext = dbContext;
        _metadata = metadata;
    }

    public int Insert(T entity)
    {
        var columnNames = string.Join(", ", _metadata.Columns.Select(c => c.ColumnName));
        
        var paramList = new List<string>();
        for (int i = 0; i < _metadata.Columns.Count; i++)
        {
            paramList.Add($"@p{i}");
        }
        var paramNames = string.Join(", ", paramList);
        
        var pkName = PrimaryKeyAttribute.ColumnName;

        string sql = $"INSERT INTO {_metadata.TableName} ({columnNames}) VALUES ({paramNames}) RETURNING {pkName};";

        using var cmd = new NpgsqlCommand(sql, _dbContext.Connection);

        for (int i = 0; i < _metadata.Columns.Count; i++)
        {
            var value = _metadata.Columns[i].Property.GetValue(entity);
            
            if (value != null)
            {
                cmd.Parameters.AddWithValue($"@p{i}", value);
            }
            else
            {
                cmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
            }
        }

        var result = cmd.ExecuteScalar();
        int newId = Convert.ToInt32(result);

        _metadata.PrimaryKeyProperty.SetValue(entity, newId);
        
        return newId;
    }

    public T FindById(int id)
    {
        string sql = $"SELECT * FROM {_metadata.TableName} WHERE {PrimaryKeyAttribute.ColumnName} = @id;";
        
        using var cmd = new NpgsqlCommand(sql, _dbContext.Connection);
        cmd.Parameters.AddWithValue("@id", id);
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapReaderToEntity(reader);
        }
        return null;
    }

    public IEnumerable<T> GetAll()
    {
        string sql = $"SELECT * FROM {_metadata.TableName};";
        using var cmd = new NpgsqlCommand(sql, _dbContext.Connection);
        using var reader = cmd.ExecuteReader();
        
        var list = new List<T>();
        while (reader.Read())
        {
            list.Add(MapReaderToEntity(reader));
        }
        return list;
    }

    public void Update(T entity)
    {
        var setList = new List<string>();
        for (int i = 0; i < _metadata.Columns.Count; i++)
        {
            setList.Add($"{_metadata.Columns[i].ColumnName} = @p{i}");
        }
        var setClauses = string.Join(", ", setList);
        
        string sql = $"UPDATE {_metadata.TableName} SET {setClauses} WHERE {PrimaryKeyAttribute.ColumnName} = @id;";

        using var cmd = new NpgsqlCommand(sql, _dbContext.Connection);
        
        for (int i = 0; i < _metadata.Columns.Count; i++)
        {
            var value = _metadata.Columns[i].Property.GetValue(entity);
            
            if (value != null)
            {
                cmd.Parameters.AddWithValue($"@p{i}", value);
            }
            else
            {
                cmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
            }
        }
        
        var idValue = _metadata.PrimaryKeyProperty.GetValue(entity);
        cmd.Parameters.AddWithValue("@id", idValue);

        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        string sql = $"DELETE FROM {_metadata.TableName} WHERE {PrimaryKeyAttribute.ColumnName} = @id;";
        
        using var cmd = new NpgsqlCommand(sql, _dbContext.Connection);
        cmd.Parameters.AddWithValue("@id", id);
        
        cmd.ExecuteNonQuery();
    }

    private T MapReaderToEntity(NpgsqlDataReader reader)
    {
        var entity = new T();

        int pkIdx = reader.GetOrdinal(PrimaryKeyAttribute.ColumnName);
        _metadata.PrimaryKeyProperty.SetValue(entity, reader.GetInt32(pkIdx));

        foreach (var col in _metadata.Columns)
        {
            int colIdx = reader.GetOrdinal(col.ColumnName);
            
            if (reader.IsDBNull(colIdx))
            {
                col.Property.SetValue(entity, null);
            }
            else
            {
                var val = reader.GetValue(colIdx);
                col.Property.SetValue(entity, val);
            }
        }

        return entity;
    }
}
