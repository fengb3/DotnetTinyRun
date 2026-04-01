// order-analysis.csx
// Run with: dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj -f ./demo/AspNetApp/scripts/order-analysis.csx
// Purpose: Analyze orders and find revenue statistics

using AspNetApp.Data;
using AspNetApp.Models;
using Microsoft.EntityFrameworkCore;

var db = new AppDbContext();
db.Database.EnsureCreated();

// Seed if empty
if (!db.Orders.Any())
{
    db.Orders.Add(new Order
    {
        CustomerName = "Alice Johnson",
        OrderDate = DateTime.Now.AddDays(-5),
        Status = OrderStatus.Delivered,
        Items =
        [
            new OrderItem { ProductId = 1, ProductName = "Laptop Pro", Quantity = 1, UnitPrice = 1299.99m },
            new OrderItem { ProductId = 2, ProductName = "Wireless Mouse", Quantity = 2, UnitPrice = 29.99m },
        ]
    });
    db.Orders.Add(new Order
    {
        CustomerName = "Bob Smith",
        OrderDate = DateTime.Now.AddDays(-2),
        Status = OrderStatus.Processing,
        Items =
        [
            new OrderItem { ProductId = 4, ProductName = "Desk Chair", Quantity = 1, UnitPrice = 349.99m },
            new OrderItem { ProductId = 5, ProductName = "Standing Desk", Quantity = 1, UnitPrice = 599.99m },
        ]
    });
    db.Orders.Add(new Order
    {
        CustomerName = "Carol Davis",
        OrderDate = DateTime.Now.AddDays(-1),
        Status = OrderStatus.Shipped,
        Items =
        [
            new OrderItem { ProductId = 6, ProductName = "Notebook", Quantity = 5, UnitPrice = 4.99m },
            new OrderItem { ProductId = 7, ProductName = "Pen Set", Quantity = 3, UnitPrice = 12.99m },
        ]
    });
    db.SaveChanges();
    Console.WriteLine("(Seeded 3 orders)\n");
}

var orders = db.Orders.Include(o => o.Items).ToList();

Console.WriteLine("=== Order Summary ===");
foreach (var order in orders)
{
    Console.WriteLine($"  #{order.Id} {order.CustomerName,-20} {order.OrderDate:d}  {order.Status,-12} Total: {order.TotalAmount:C}");
    foreach (var item in order.Items)
        Console.WriteLine($"       - {item.ProductName} x{item.Quantity} @ {item.UnitPrice:C}");
}

// Revenue by status
var revenueByStatus = orders
    .GroupBy(o => o.Status)
    .Select(g => new { Status = g.Key, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
    .OrderBy(g => g.Status);

Console.WriteLine("\n=== Revenue by Status ===");
foreach (var s in revenueByStatus)
    Console.WriteLine($"  {s.Status,-12} {s.Count} orders  Revenue: {s.Revenue:C}");

Console.WriteLine($"\nTotal Revenue: {orders.Sum(o => o.TotalAmount):C}");
