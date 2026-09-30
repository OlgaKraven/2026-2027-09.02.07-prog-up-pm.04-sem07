namespace Shop;
public static class RepairLogic
{
    // D1: пользователь определяется только по зарегистрированному логину.
    public static User? Authenticate(StoreData data, string login)
    {
        return data.Users.FirstOrDefault(u => u.Login == login.Trim());
    }

    // D2: переход «Назад» сохраняет неподтверждённый заказ.
    public static void Back(List<CartLine> cart)
    {
        // Очистка выполняется отдельной подтверждаемой командой отмены.
    }

    // D3: поиск и категория сужают один набор; цена сравнивается как число.
    public static List<Product> Catalog(StoreData data, string query, string category, bool descending)
    {
        query = query.Trim();
        var filtered = data.Products.Where(p =>
            (category == "Все категории" || p.Category == category) &&
            (p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        return descending ? filtered.OrderByDescending(p => p.DiscountPrice).ThenBy(p => p.Id).ToList()
                          : filtered.OrderBy(p => p.DiscountPrice).ThenBy(p => p.Id).ToList();
    }

    // D4: проверяется общий объём уже добавленных позиций одного размера.
    public static string ValidateQuantity(Product product, string size, string input, List<CartLine> cart)
    {
        if (!int.TryParse(input, out var quantity) || quantity <= 0)
            return "Количество должно быть положительным целым числом.";
        if (!product.Stock.TryGetValue(size, out var stock) || stock <= 0)
            return "Выберите размер, доступный для заказа.";
        var reserved = cart.Where(x => x.ProductId == product.Id && x.Size == size).Sum(x => (long)x.Quantity);
        return quantity + (long)reserved > stock ? "Количество в заказе превышает остаток выбранного размера." : "";
    }

    // D5: сначала проверяется весь заказ, затем изменяются все остатки.
    public static string Commit(StoreData data, User user, List<CartLine> cart, DateTime date)
    {
        if (cart.Count == 0) return "Добавьте хотя бы одну позицию в заказ.";
        if (cart.Any(x => x.Quantity <= 0))
            return "Количество каждой позиции должно быть положительным.";
        foreach (var group in cart.GroupBy(x => (x.ProductId, x.Size)))
        {
            var matches = data.Products.Where(p => p.Id == group.Key.ProductId).ToList();
            if (matches.Count != 1) return "Товар отсутствует или его идентификатор не уникален. Проверьте данные заказа.";
            var product = matches[0];
            if (!product.Stock.TryGetValue(group.Key.Size, out var stock) || group.Sum(x => (long)x.Quantity) > stock)
                return "Остатки изменились. Уменьшите количество или отмените заказ.";
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
