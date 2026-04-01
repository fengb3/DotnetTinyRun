namespace AspNetApp.Models;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount => Items.Sum(i => i.Quantity * i.UnitPrice);

    public override string ToString() =>
        $"Order {{ Id={Id}, Customer={CustomerName}, Date={OrderDate:d}, Total={TotalAmount:C}, Status={Status} }}";
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
