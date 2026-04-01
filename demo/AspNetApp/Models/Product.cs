namespace AspNetApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public override string ToString() =>
        $"Product {{ Id={Id}, Name={Name}, Category={Category}, Price={Price:C}, Stock={Stock} }}";
}
