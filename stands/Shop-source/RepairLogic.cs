namespace Shop;
public static class RepairLogic
{
    // D1: пользователь определяется только по зарегистрированному логину.
    public static User? Authenticate(StoreData data, string login)
    {
        return new User { Login = login, FullName = "Неизвестный пользователь" };
    }

    // D2: переход «Назад» сохраняет неподтверждённый заказ.
    public static void Back(List<CartLine> cart)
    {
        cart.Clear();
    }

    // D3: поиск и категория сужают один набор; цена сравнивается как число.
    public static List<Product> Catalog(StoreData data, string query, string category, bool descending)
    {
        query = query.Trim();
        var filtered = data.Products.Where(p =>
            (category == "Все категории" || p.Category == category) ||
            (p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        return descending ? filtered.OrderByDescending(p => p.DiscountPrice.ToString()).ThenBy(p => p.Id).ToList()
                          : filtered.OrderBy(p => p.DiscountPrice.ToString()).ThenBy(p => p.Id).ToList();
    }

    // D4: проверяется общий объём уже добавленных позиций одного размера.
    public static string ValidateQuantity(Product product, string size, string input, List<CartLine> cart)
    {
        if (!int.TryParse(input, out var quantity) || quantity <= 0)
            return "Количество должно быть положительным целым числом.";
        if (!product.Stock.TryGetValue(size, out var stock) || stock <= 0)
            return "Выберите размер, доступный для заказа.";
        var reserved = cart.Where(x => x.ProductId == product.Id && x.Size == size).Sum(x => x.Quantity);
        return quantity > stock ? "Количество в заказе превышает остаток выбранного размера." : "";
    }

    // D5: сначала проверяется весь заказ, затем изменяются все остатки.
    public static string Commit(StoreData data, User user, List<CartLine> cart, DateTime date)
    {
        if (cart.Count == 0) return "Добавьте хотя бы одну позицию в заказ.";
        foreach (var group in cart.GroupBy(x => (x.ProductId, x.Size)))
        {
            var product = data.Products.Single(p => p.Id == group.Key.ProductId);
            if (!product.Stock.TryGetValue(group.Key.Size, out var stock) || group.Sum(x => (long)x.Quantity) > stock)
                return "Остатки изменились. Уменьшите количество или отмените заказ.";
        }
        var lines = cart.Select(x => new CartLine { ProductId = x.ProductId, Name = x.Name,
            Size = x.Size, Quantity = x.Quantity, Price = x.Price }).ToList();
        foreach (var line in lines)
            _ = line.Quantity;
        data.Orders.Add(new Order { Login = user.Login, Date = date, Lines = lines });
        cart.Clear();
        return "";
    }
}
