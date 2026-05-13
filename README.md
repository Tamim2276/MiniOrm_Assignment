# Scratch ORM with ADO.NET

This repository contains a custom, lightweight ORM (Object-Relational Mapper) built from scratch using C#, ADO.NET, and Npgsql. It features a generic `DbSet<T>` for dynamic CRUD operations, C# reflection for real-time type mapping to PostgreSQL, and a standalone command-line Migrations tool.

## Prerequisites & PostgreSQL Setup

1. You need a running instance of **PostgreSQL** (local or hosted).
2. Create a fresh testing database:
   ```sql
   CREATE DATABASE miniorm_test;
   ```

## Setting the Connection String

Our ORM dynamically connects using the environment variable `MINIORM_CONN`. Securely set it in your terminal before running the application.

**On Windows (Command Prompt):**

```cmd
set MINIORM_CONN=Host=localhost;Database=miniorm_test;Username=postgres;Password=yourpassword
```

**On Windows (PowerShell):**

```powershell
$env:MINIORM_CONN="Host=localhost;Database=miniorm_test;Username=postgres;Password=yourpassword"
```

**On macOS/Linux:**

```bash
export MINIORM_CONN="Host=localhost;Database=miniorm_test;Username=postgres;Password=yourpassword"
```

## Running the Migrations

The `MiniOrm.Migrations` project acts as a CLI for detecting models and managing your database schema.

Navigate to the migrations folder:

```bash
cd MiniOrm.Migrations
```

1. **Add a Migration** (Generates an up/down `.sql` script):
   ```bash
   dotnet run -- migrations add InitialCreate
   ```
2. **Apply Pending Migrations** (Executes to the live Postgres DB):
   ```bash
   dotnet run -- migrations apply
   ```
3. **List Migrations** (Shows applied vs pending status):
   ```bash
   dotnet run -- migrations list
   ```
4. **Rollback** (Reverts the most recently applied migration):
   ```bash
   dotnet run -- migrations rollback
   ```

## Running the Demo

Once migrations are applied and the tables `products` and `orders` exist, run the demo application to test the full CRUD layer.

Navigate to the main directory and run:

```bash
cd MiniOrm
dotnet run
```

This triggers an end-to-end walkthrough establishing the `AppDbContext`, inserting a keyboard record, updating its pricing information, querying the state, and ultimately cleaning up with a delete command.

## Type Mapping & Attribute Filtering (How it Works)

The ORM leverages **C# Reflection** in `TypeMapper.cs` to figure out mappings at runtime.

- **Attribute Filtering**: It strictly looks for `[Table("name")]` on classes, and only observes properties tagged with `[PrimaryKey]` or `[Column("name")]`. If a property lacks these attributes (such as computed properties or navigation properties), it is completely ignored, successfully segregating application logic from database persistence.
- **Nullability vs Explicit Types**: We utilize `Nullable.GetUnderlyingType()` to detect options like `decimal?`. C# `decimal` translates directly to PostgreSQL `NUMERIC NOT NULL`, whereas `decimal?` gracefully translates to `NUMERIC NULL`.
- **String Memory Integration**: Standard `string` converts to `TEXT NOT NULL`.
- C# `null` values assigned to instances are dynamically mapped to `DBNull.Value` before execution to ensure flawless ADO.NET transit.
# MiniOrm_Assignment
