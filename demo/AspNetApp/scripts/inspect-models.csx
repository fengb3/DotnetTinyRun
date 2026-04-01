// inspect-models.csx
// Run with: dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj -f ./demo/AspNetApp/scripts/inspect-models.csx
// Purpose: Use reflection to inspect ASP.NET models and DbContext structure

using AspNetApp.Data;
using AspNetApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

// Reflect on model types
var modelTypes = new[] { typeof(Product), typeof(Order), typeof(OrderItem) };

Console.WriteLine("=== ASP.NET Data Model Inspection ===\n");
foreach (var type in modelTypes)
{
    Console.WriteLine($"[{type.Name}]");
    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        var nullable = prop.PropertyType.IsGenericType &&
                       prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        var typeName = nullable
            ? $"{Nullable.GetUnderlyingType(prop.PropertyType)!.Name}?"
            : prop.PropertyType.Name;
        Console.WriteLine($"  {typeName,-25} {prop.Name}");
    }
    Console.WriteLine();
}

// Reflect on DbContext DbSets
Console.WriteLine("[AppDbContext DbSets]");
foreach (var prop in typeof(AppDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.PropertyType.IsGenericType &&
                         p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
{
    var entityType = prop.PropertyType.GetGenericArguments()[0];
    Console.WriteLine($"  DbSet<{entityType.Name}> -> {prop.Name}");
}
