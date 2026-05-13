using System;
using MiniOrm.Data;
using MiniOrm.Models;
using System.Collections.Generic;

namespace MiniOrm;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }

    public AppDbContext(string connStr) : base(connStr)
    {
        Products = new DbSet<Product>(this, TypeMapper.BuildMetadata<Product>());
        Orders = new DbSet<Order>(this, TypeMapper.BuildMetadata<Order>());
    }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Step 1 & 2: Entity and AppDbContext defined.");
        
        string? connStr = Environment.GetEnvironmentVariable("MINIORM_CONN");
        if (string.IsNullOrEmpty(connStr))
        {
            Console.WriteLine("Please set the MINIORM_CONN environment variable.");
            return;
        }

        using var db = new AppDbContext(connStr);
        Console.WriteLine("Step 3: DbContext created. (Remember to run migrations from MiniOrm.Migrations!)");

        var keyboard = new Product 
        { 
            Name = "Keyboard", 
            Price = 89.99m, 
            Discount = null, 
            InStock = true 
        };
        
        int id = db.Products.Insert(keyboard);
        Console.WriteLine($"Inserted Product Id={id}, Discount={(keyboard.Discount.HasValue ? keyboard.Discount.Value.ToString() : "NULL")} ");
        var order = new Order 
        { 
            OrderDate = DateTime.Now, 
            CustomerName = "Tamim", 
            TotalAmount = 150.50m 
        };
        db.Orders.Insert(order);
        Console.WriteLine("Inserted a test Order! Check pgAdmin to see it.");
        var found = db.Products.FindById(id);
        if (found != null)
        {
            Console.WriteLine($"Found → {found.Name}, Price={found.Price}, Discount={(found.Discount.HasValue ? found.Discount.Value.ToString() : "NULL")}");
            
            found.Price = 79.99m; 
            found.Discount = 5.00m;
            db.Products.Update(found);
            Console.WriteLine($"Updated → Price={found.Price}, Discount={found.Discount} ");
        }

        IEnumerable<Product> allProducts = db.Products.GetAll();
        int count = 0;
        foreach (var p in allProducts)
        {
            count++;
        }
        Console.WriteLine($"Products in database: {count}");

        // db.Products.Delete(id);
        // Console.WriteLine($"Deleted Id={id}");

        IEnumerable<Product> remainingProducts = db.Products.GetAll();
        int remainingCount = 0;
        foreach (var p in remainingProducts)
        {
            remainingCount++;
        }
        Console.WriteLine($"{remainingCount} products remaining");
    }
}