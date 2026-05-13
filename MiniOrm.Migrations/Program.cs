using System;
using MiniOrm.Migrations.Commands;

namespace MiniOrm.Migrations;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- migrations [command]");
            return;
        }

        if (args[0] == "migrations")
        {
            if (args.Length > 1)
            {
                string command = args[1];
                var runner = new MigrationRunner();

                if (command == "add")
                {
                    if (args.Length > 2)
                    {
                        string name = args[2];
                        runner.AddMigration(name);
                    }
                    else
                    {
                        Console.WriteLine("Please provide a name for the migration.");
                    }
                }
                else if (command == "apply")
                {
                    try
                    {
                        runner.ApplyMigrations();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error applying: " + ex.Message);
                    }
                }
                else if (command == "list")
                {
                    try
                    {
                        runner.ListMigrations();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error listing: " + ex.Message);
                    }
                }
                else if (command == "rollback")
                {
                    try
                    {
                        runner.RollbackMigration();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error rolling back: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown command: {command}");
                }
            }
            else
            {
                Console.WriteLine("Please specify a command: add, apply, list, rollback");
            }
        }
        else
        {
            Console.WriteLine("Command not recognized.");
        }
    }
}