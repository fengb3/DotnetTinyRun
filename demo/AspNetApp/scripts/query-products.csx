// query-products.csx
// Run with: dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj -f ./demo/AspNetApp/scripts/query-products.csx
// Purpose: Query the product catalog and show inventory statistics

using AspNetApp.Data;
using AspNetApp.Models;

var db = new AppDbContext();
db.Database.EnsureCreated();

// Seed data if empty
if (!db.Products.Any())
{
    db.Products.AddRange(
        new Product { Name = "Laptop Pro", Category = "Electronics", Price = 1299.99m, Stock = 50 },
        new Product { Name = "Wireless Mouse", Category = "Electronics", Price = 29.99m, Stock = 200 },
        new Product { Name = "USB Hub", Category = "Electronics", Price = 49.99m, Stock = 8 },
        new Product { Name = "Desk Chair", Category = "Furniture", Price = 349.99m, Stock = 25 },
        new Product { Name = "Standing Desk", Category = "Furniture", Price = 599.99m, Stock = 5 },
        new Product { Name = "Notebook", Category = "Stationery", Price = 4.99m, Stock = 500 },
        new Product { Name = "Pen Set", Category = "Stationery", Price = 12.99m, Stock = 300 },
        new Product { Name = "Coffee Mug", Category = "Kitchen", Price = 14.99m, Stock = 150 },
        new Product { Name = "Water Bottle", Category = "Kitchen", Price = 24.99m, Stock = 7 },
        new Product { Name = "Headphones", Category = "Electronics", Price = 199.99m, Stock = 3 }
    );
    db.SaveChanges();
    Console.WriteLine("(Seeded 10 products)\n");
}

// Show all products grouped by category
var byCategory = db.Products
    .GroupBy(p => p.Category)
    .Select(g => new { Category = g.Key, Count = g.Count(), TotalValue = g.Sum(p => p.Price * p.Stock) })
    .OrderBy(g => g.Category)
    .ToList();

Console.WriteLine("=== Inventory by Category ===");
foreach (var cat in byCategory)
    Console.WriteLine($"  {cat.Category,-15} {cat.Count} products  Value: {cat.TotalValue:C}");

// Show low-stock items (stock <= 10)
var lowStock = db.Products.Where(p => p.Stock <= 10 && p.IsActive).OrderBy(p => p.Stock).ToList();
Console.WriteLine($"\n=== Low Stock Alert ({lowStock.Count} items) ===");
foreach (var p in lowStock)
    Console.WriteLine($"  [{p.Stock,3} left] {p.Name} ({p.Category}) @ {p.Price:C}");

// Total inventory value
var totalValue = db.Products.Where(p => p.IsActive).Sum(p => p.Price * p.Stock);
Console.WriteLine($"\nTotal Inventory Value: {totalValue:C}");
