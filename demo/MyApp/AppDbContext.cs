using Microsoft.EntityFrameworkCore;

namespace MyApp;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=demo.db");
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }

    public override string ToString() => $"User {{ Id = {Id}, Name = {Name}, Email = {Email}, Age = {Age} }}";
}
