using AspNetApp.Data;
using AspNetApp.Models;
using AspNetApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

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

        db.SaveChanges();
        Console.WriteLine("Database seeded.");
    }
}

// Minimal API endpoints
app.MapGet("/api/products", (AppDbContext db) => db.Products.ToList());
app.MapGet("/api/products/{id}", (int id, AppDbContext db) => db.Products.Find(id));
app.MapGet("/api/orders", (AppDbContext db) => db.Orders.ToList());
app.MapGet("/api/products/low-stock", (AppDbContext db, ProductService svc) => svc.GetLowStock());

app.Run();
