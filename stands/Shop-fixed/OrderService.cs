namespace Shop;
// Готовая операция. В учебной работе её не требуется реализовывать заново.
public static class OrderService
{
    public static string ValidateQuantity(Product product, string size, string input, List<CartLine> cart)
    {
        if (!int.TryParse(input, out var quantity) || quantity <= 0)
            return "Поле «Количество»: требуется положительное целое число.";
        if (!product.Stock.TryGetValue(size, out var stock) || stock <= 0)
            return "Поле «Исполнение»: выбрать доступное исполнение.";
        var reserved = cart.Where(x => x.ProductId == product.Id && x.Size == size).Sum(x => (long)x.Quantity);
        return quantity + (long)reserved > stock
            ? "Поле «Количество»: уменьшить значение с учётом уже добавленных позиций." : "";
    }
    public static string Commit(StoreData data, User user, List<CartLine> cart, DateTime date)
    {
        if (!data.Users.Any(u => u.Login == user.Login)) return "Требуется зарегистрированный пользователь.";
        if (cart.Count == 0) return "Добавить хотя бы одну позицию в заявку.";
        if (cart.Any(x => x.Quantity <= 0)) return "Количество каждой позиции должно быть положительным.";
        foreach (var group in cart.GroupBy(x => (x.ProductId, x.Size)))
        {
            var matches = data.Products.Where(p => p.Id == group.Key.ProductId).ToList();
            if (matches.Count != 1) return "Позиция отсутствует или её Id не уникален.";
            if (!matches[0].Stock.TryGetValue(group.Key.Size, out var stock) || group.Sum(x => (long)x.Quantity) > stock)
                return "Доступное количество изменилось. Уменьшить количество или отменить заявку.";
        }
        var lines = cart.Select(x => new CartLine { ProductId = x.ProductId, Name = x.Name,
            Size = x.Size, Quantity = x.Quantity, Price = x.Price }).ToList();
        foreach (var line in lines)
            data.Products.Single(p => p.Id == line.ProductId).Stock[line.Size] -= line.Quantity;
        data.Orders.Add(new Order { Login = user.Login, Date = date, Lines = lines });
        cart.Clear();
        return "";
    }
}
