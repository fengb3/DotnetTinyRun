using Microsoft.EntityFrameworkCore;

namespace MyApp;

class Program
{
    static void Main()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { Name = "Alice", Email = "alice@example.com", Age = 28 },
                new User { Name = "Bob", Email = "bob@example.com", Age = 35 },
                new User { Name = "Charlie", Email = "charlie@example.com", Age = 22 },
                new User { Name = "Diana", Email = "diana@example.com", Age = 31 },
                new User { Name = "Eve", Email = "eve@example.com", Age = 26 }
            );
            db.SaveChanges();
            Console.WriteLine("Database seeded with 5 users.");
        }
        else
        {
            Console.WriteLine($"Database already has {db.Users.Count()} users.");
        }
    }
}
