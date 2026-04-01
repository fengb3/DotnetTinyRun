using AspNetApp.Data;
using AspNetApp.Models;

namespace AspNetApp.Services;

public class ProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Product> GetLowStock(int threshold = 10) =>
        _db.Products.Where(p => p.Stock <= threshold && p.IsActive).ToList();

    public IEnumerable<Product> GetByCategory(string category) =>
        _db.Products.Where(p => p.Category == category && p.IsActive).ToList();

    public decimal GetInventoryValue() =>
        _db.Products.Where(p => p.IsActive).Sum(p => p.Price * p.Stock);

    public Dictionary<string, int> GetStockByCategory() =>
        _db.Products
            .Where(p => p.IsActive)
            .GroupBy(p => p.Category)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Stock));
}
