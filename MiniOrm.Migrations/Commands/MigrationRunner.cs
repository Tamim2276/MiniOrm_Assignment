using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MiniOrm.Attributes;
using MiniOrm.Data;
using Npgsql;

namespace MiniOrm.Migrations.Commands;

public class MigrationRunner
{
    private readonly string _migrationsDir;

    public MigrationRunner()
    {
        _migrationsDir = Path.Combine(Directory.GetCurrentDirectory(), "MigrationScripts");

        if (Directory.Exists(_migrationsDir) == false)
        {
            Directory.CreateDirectory(_migrationsDir);
        }
    }

    public void AddMigration(string name)
    {
        var upSql = new StringBuilder();
        upSql.AppendLine("-- up");
        
        var downSql = new StringBuilder();
        downSql.AppendLine("-- down");

        Assembly targetAssembly = typeof(TypeMapper).Assembly;
        Type[] allTypes = targetAssembly.GetTypes();
        
        var entityTypes = new List<Type>();
        for (int i = 0; i < allTypes.Length; i++)
        {
            Type currentType = allTypes[i];
            var tableAttr = currentType.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null)
            {
                entityTypes.Add(currentType);
            }
        }

        foreach (var type in entityTypes)
        {
            MethodInfo? method = typeof(TypeMapper).GetMethod("BuildMetadata");
            if (method != null)
            {
                MethodInfo genericMethod = method.MakeGenericMethod(type);
                var metaObj = genericMethod.Invoke(null, null);
                
                if (metaObj != null)
                {
                    var meta = (EntityMetadata)metaObj;

                    // Generate Up
                    upSql.AppendLine($"CREATE TABLE IF NOT EXISTS {meta.TableName} (");
                    
                    var pkPgType = TypeMapper.GetPostgresColumnDefinition(meta.PrimaryKeyProperty, true);
                    string pkColName = PrimaryKeyAttribute.ColumnName;
                    
                    if (meta.Columns.Count > 0)
                    {
                        upSql.AppendLine($"  {pkColName} {pkPgType},");
                    }
                    else
                    {
                        upSql.AppendLine($"  {pkColName} {pkPgType}");
                    }

                    for (int i = 0; i < meta.Columns.Count; i++)
                    {
                        var col = meta.Columns[i];
                        var pgType = TypeMapper.GetPostgresColumnDefinition(col.Property, false);
                        
                        string comma = "";
                        if (i < meta.Columns.Count - 1)
                        {
                            comma = ",";
                        }
                        
                        upSql.AppendLine($"  {col.ColumnName} {pgType}{comma}");
                    }
                    upSql.AppendLine(");");
                    upSql.AppendLine();

                    // Generate Down
                    downSql.AppendLine($"DROP TABLE IF EXISTS {meta.TableName};");
                }
            }
        }

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string filename = $"{timestamp}_{name}.sql";
        string filePath = Path.Combine(_migrationsDir, filename);

        var fullContent = new StringBuilder();
        fullContent.AppendLine(upSql.ToString().TrimEnd());
        fullContent.AppendLine(downSql.ToString().TrimEnd());

        File.WriteAllText(filePath, fullContent.ToString());
        Console.WriteLine($"Created migration script at: {filePath}");
    }

    private string GetConnectionString()
    {
        string? conn = Environment.GetEnvironmentVariable("MINIORM_CONN");
        if (string.IsNullOrEmpty(conn))
        {
            throw new Exception("Environment variable MINIORM_CONN is not set.");
        }
        return conn;
    }

    private void EnsureMigrationsTable(NpgsqlConnection conn)
    {
        string sql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL,
                applied_on TIMESTAMP NOT NULL
            );
        ";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private List<string> GetAppliedMigrations(NpgsqlConnection conn)
    {
        var applied = new List<string>();
        string sql = "SELECT name FROM __migrations ORDER BY id ASC;";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            applied.Add(reader.GetString(0));
        }
        return applied;
    }

    public void ApplyMigrations()
    {
        string connStr = GetConnectionString();
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        EnsureMigrationsTable(conn);
        List<string> applied = GetAppliedMigrations(conn);

        string[] files = Directory.GetFiles(_migrationsDir, "*.sql");
        Array.Sort(files);

        int appliedCount = 0;
        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            string fileName = Path.GetFileName(filePath);

            if (applied.Contains(fileName) == false)
            {
                Console.WriteLine($"Applying {fileName}...");
                string[] lines = File.ReadAllLines(filePath);
                var upScript = new StringBuilder();
                bool inUp = false;

                for (int j = 0; j < lines.Length; j++)
                {
                    string line = lines[j];
                    if (line.StartsWith("-- up"))
                    {
                        inUp = true;
                        continue;
                    }
                    else if (line.StartsWith("-- down"))
                    {
                        inUp = false;
                        continue;
                    }

                    if (inUp)
                    {
                        upScript.AppendLine(line);
                    }
                }

                using var cmd = new NpgsqlCommand(upScript.ToString(), conn);
                cmd.ExecuteNonQuery();

                string insertSql = "INSERT INTO __migrations (name, applied_on) VALUES (@name, @date);";
                using var insertCmd = new NpgsqlCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@name", fileName);
                insertCmd.Parameters.AddWithValue("@date", DateTime.Now);
                insertCmd.ExecuteNonQuery();

                appliedCount++;
            }
        }

        if (appliedCount == 0)
        {
            Console.WriteLine("No pending migrations to apply.");
        }
        else
        {
            Console.WriteLine($"Successfully applied {appliedCount} migrations.");
        }
    }

    public void ListMigrations()
    {
        string connStr = GetConnectionString();
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        EnsureMigrationsTable(conn);
        List<string> applied = GetAppliedMigrations(conn);

        string[] files = Directory.GetFiles(_migrationsDir, "*.sql");
        Array.Sort(files);

        if (files.Length == 0)
        {
            Console.WriteLine("No migrations found.");
            return;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            if (applied.Contains(fileName))
            {
                Console.WriteLine($"[applied] {fileName}");
            }
            else
            {
                Console.WriteLine($"[pending] {fileName}");
            }
        }
    }

    public void RollbackMigration()
    {
        string connStr = GetConnectionString();
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        EnsureMigrationsTable(conn);

        string lastMigration = "";
        int lastId = -1;

        string sql = "SELECT id, name FROM __migrations ORDER BY id DESC LIMIT 1;";
        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                lastId = reader.GetInt32(0);
                lastMigration = reader.GetString(1);
            }
        }

        if (lastId == -1)
        {
            Console.WriteLine("No applied migrations to rollback.");
            return;
        }

        Console.WriteLine($"Rolling back {lastMigration}...");
        string filePath = Path.Combine(_migrationsDir, lastMigration);

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            var downScript = new StringBuilder();
            bool inDown = false;

            for (int j = 0; j < lines.Length; j++)
            {
                string line = lines[j];
                if (line.StartsWith("-- down"))
                {
                    inDown = true;
                    continue;
                }
                else if (line.StartsWith("-- up"))
                {
                    inDown = false;
                    continue;
                }

                if (inDown)
                {
                    downScript.AppendLine(line);
                }
            }

            using var cmd = new NpgsqlCommand(downScript.ToString(), conn);
            cmd.ExecuteNonQuery();
        }
        else
        {
            Console.WriteLine($"Warning: File {lastMigration} not found. Execute down script manually.");
        }

        string delSql = "DELETE FROM __migrations WHERE id = @id;";
        using (var cmd = new NpgsqlCommand(delSql, conn))
        {
            cmd.Parameters.AddWithValue("@id", lastId);
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine($"Rollback of {lastMigration} successful.");
    }
}
