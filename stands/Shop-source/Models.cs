namespace Shop;
public sealed class User
{
    public string Login { get; set; } = "";
    public string FullName { get; set; } = "";
}
public sealed class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Composition { get; set; } = "";
    public decimal Price { get; set; }
    public Dictionary<string, int> Stock { get; set; } = new();
    public string ImagePath { get; set; } = "";
    public decimal DiscountPrice { get; set; }
    public int Available => Stock.Values.Sum();
}
public sealed class CartLine
{
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Size { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
}
public sealed class Order
{
    public string Login { get; set; } = "";
    public DateTime Date { get; set; }
    public List<CartLine> Lines { get; set; } = new();
}
public sealed class StoreData
{
    public string Subject { get; set; } = "";
    public List<User> Users { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}
