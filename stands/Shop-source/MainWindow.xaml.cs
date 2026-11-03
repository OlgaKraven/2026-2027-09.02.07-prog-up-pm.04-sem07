using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Globalization;
namespace Shop;
public sealed class LowStockConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is int stock && stock <= 3;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
public partial class MainWindow : Window
{
    private StoreData data = new();
    private User? user;
    private readonly List<CartLine> cart = new();
    private Product? selected;
    private bool ready;
    private readonly string dataPath;
    private static readonly JsonSerializerOptions json = new() { WriteIndented = true };
    public MainWindow()
    {
        InitializeComponent();
        var args = Environment.GetCommandLineArgs();
        dataPath = Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "data.json"));
        try
        {
            data = JsonSerializer.Deserialize<StoreData>(File.ReadAllText(dataPath)) ?? throw new InvalidDataException("Пустой набор данных");
            SubjectText.Text = data.Subject;
            CategoryBox.ItemsSource = new[] { "Все категории" }.Concat(data.Products.Select(p => p.Category).Distinct()).ToList();
            CategoryBox.SelectedIndex = 0;
            SortBox.SelectedIndex = 0;
            ready = true;
            RefreshCatalog();
            SetAccess();
            StatusText.Text = "Откройте позицию кнопкой в строке каталога. Для заявки войдите по зарегистрированному логину.";
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            Message("Не удалось прочитать данные. Проверьте наличие и формат data.json. " + ex.Message, true);
            IsEnabled = false;
        }
    }
    private void Message(string text, bool error = false)
    {
        StatusText.Text = text;
        MessageBox.Show(this, text, error ? "Ошибка" : "Информация", MessageBoxButton.OK,
            error ? MessageBoxImage.Error : MessageBoxImage.Information);
    }
    private void SetAccess()
    {
        SearchBox.IsEnabled = CategoryBox.IsEnabled = SortBox.IsEnabled = user != null;
        UserText.Text = user?.FullName ?? "Гость";
        LoginBox.IsEnabled = LoginButton.IsEnabled = user == null;
    }
    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (user != null)
        {
            Message("Для смены пользователя сначала нажмите «Выйти». Неподтверждённая заявка отменяется только с вашего согласия.", true);
            return;
        }
        user = RepairLogic.Authenticate(data.Users, LoginBox.Text);
        SetAccess();
        if (user == null) Message("Логин не найден. Введите логин из предоставленного списка пользователей.", true);
        else StatusText.Text = "Вход выполнен: " + user.FullName;
    }
    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (cart.Count > 0 && MessageBox.Show(this, "При выходе неподтверждённая заявка будет отменена. Продолжить?",
            "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        cart.Clear();
        user = null;
        LoginBox.Clear();
        SearchBox.Clear();
        CategoryBox.SelectedIndex = SortBox.SelectedIndex = 0;
        SetAccess();
        ShowCatalog();
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (ready) RefreshCatalog(); }
    private void Options_Changed(object sender, SelectionChangedEventArgs e) { if (ready) RefreshCatalog(); }
    private void RefreshCatalog()
    {
        foreach (var p in data.Products) p.DiscountPrice = decimal.Round(p.Price, 2);
        ProductsGrid.ItemsSource = RepairLogic.Catalog(data.Products, SearchBox.Text, CategoryBox.SelectedItem?.ToString() ?? "Все категории", SortBox.SelectedIndex == 1);
        ProductsGrid.UpdateLayout();
    }
    private void Product_Open(object sender, MouseButtonEventArgs e) { if (ProductsGrid.SelectedItem is Product p) OpenProduct(p); }
    private void Product_Button(object sender, RoutedEventArgs e) { if (((Button)sender).Tag is Product p) OpenProduct(p); }
    private void OpenProduct(Product product)
    {
        selected = product;
        DetailsText.Text = $"{product.Name} · {product.Manufacturer}\nКатегория: {product.Category}; стоимость: {product.DiscountPrice:F2} руб.\nНазначение: {product.Composition}\n{product.Description}\nИсполнения: {string.Join(", ", product.Stock.Select(x => $"{x.Key}: {x.Value}"))}";
        SizeBox.ItemsSource = product.Stock.Where(x => x.Value > 0).Select(x => x.Key).ToList();
        SizeBox.SelectedIndex = 0;
        QuantityBox.Text = "1";
        var imagePath = Path.Combine(Path.GetDirectoryName(dataPath)!, product.ImagePath);
        var fallback = Path.Combine(AppContext.BaseDirectory, "picture.png");
        try { ProductImage.Source = new BitmapImage(new Uri(File.Exists(imagePath) && !string.IsNullOrEmpty(product.ImagePath) ? imagePath : fallback)); }
        catch (Exception ex) when (ex is IOException or NotSupportedException) { ProductImage.Source = null; }
        CartGrid.ItemsSource = cart.ToList();
        CatalogPanel.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility = Visibility.Visible;
        Title = "Сервис — позиция и заявка";
    }
    private void ShowCatalog()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        CatalogPanel.Visibility = Visibility.Visible;
        Title = "Сервис — каталог";
        RefreshCatalog();
    }
    private void Back_Click(object sender, RoutedEventArgs e) { RepairLogic.Back(cart); ShowCatalog(); }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (user == null) { Message("Для оформления заявки войдите по зарегистрированному логину.", true); return; }
        if (selected == null) return;
        var size = SizeBox.SelectedItem?.ToString() ?? "";
        var error = OrderService.ValidateQuantity(selected, size, QuantityBox.Text, cart);
        if (error != "") { Message(error, true); return; }
        cart.Add(new CartLine { ProductId = selected.Id, Name = selected.Name, Size = size,
            Quantity = int.Parse(QuantityBox.Text), Price = selected.DiscountPrice });
        CartGrid.ItemsSource = cart.ToList();
        StatusText.Text = $"Добавлено в заявку: {selected.Name}. Позиций: {cart.Count}. Итог: {cart.Sum(x => x.Total):F2} руб.";
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (cart.Count == 0) { StatusText.Text = "Неподтверждённая заявка пуста."; return; }
        if (MessageBox.Show(this, "Отменить неподтверждённую заявку? Остатки не изменятся.", "Предупреждение",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        cart.Clear();
        ShowCatalog();
        StatusText.Text = "Заявка отменена. Остатки сохранены.";
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (user == null) { Message("Для подтверждения заявки необходимо войти.", true); return; }
        // Сначала изменяется копия. При ошибке сохранения исходные данные остаются доступны.
        var candidate = JsonSerializer.Deserialize<StoreData>(JsonSerializer.Serialize(data))!;
        var error = OrderService.Commit(candidate, user, cart.ToList(), DateTime.Now);
        if (error != "") { Message(error, true); return; }
        try
        {
            File.WriteAllText(dataPath + ".tmp", JsonSerializer.Serialize(candidate, json));
            File.Move(dataPath + ".tmp", dataPath, true);
            data = candidate;
            cart.Clear();
            ShowCatalog();
            Message("Заявка оформлена. Дата сохранена, остатки каталога обновлены.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Message("Не удалось сохранить заявку. Проверьте доступ к рабочей папке и повторите подтверждение. " + ex.Message, true); }
    }
}
