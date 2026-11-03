namespace Shop;
// Договорённости для переноса диагностических компонентов на другую модель.
public interface IRegisteredUser { string Login { get; } }
public interface ICatalogItem
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    decimal DiscountPrice { get; }
}
