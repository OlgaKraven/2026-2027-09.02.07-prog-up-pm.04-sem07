# Контракт диагностических компонентов

Переносить ComponentContracts.cs и принятый RepairLogic.cs. Контракт не требует StoreData.

- IRegisteredUser.Login: непустой уникальный логин; null у результата Authenticate означает отказ. Сравнение чувствительно к регистру, крайние пробелы ввода удаляются.
- ICatalogItem: Id (уникальный), Name, Description, Category (не null), DiscountPrice (decimal, рассчитан вызывающим кодом). Поиск только по Name/Description, без Manufacturer. Категория сравнивается точно; «Все категории» отключает только фильтр категории. Сортировка стоимости числовая, второй ключ Id возрастающий в обоих направлениях.
- Back принимает ICollection<TLine> и не меняет её. Отмена и выход — отдельные операции вызывающей стороны.
- Коллекции не null; Catalog возвращает новый список тех же объектов и не меняет входные данные. Состояние пользовательского интерфейса сохраняет вызывающий код.

Пример другой модели (не индивидуальное решение):

```csharp
public sealed class Sensor : Shop.ICatalogItem
{
    public string Code { get; init; } = "";
    public string Label { get; init; } = "";
    public string Notes { get; init; } = "";
    public string Group { get; init; } = "";
    public decimal ServiceCost { get; init; }
    public string Id => Code;
    public string Name => Label;
    public string Description => Notes;
    public string Category => Group;
    public decimal DiscountPrice => ServiceCost;
}
// var result = Shop.RepairLogic.Catalog(sensors, "диагностика", "Датчики", false);
```

OrderService — отдельная готовая операция модели этого стенда. ValidateQuantity возвращает пустую строку при успехе или сообщение с полем при отказе, не меняет заявку и остатки. Commit проверяет все позиции до изменения, записывает заявку и уменьшает остатки; при отказе изменений нет. Confirm_Click применяет Commit к копии, заменяет рабочие данные только после записи файла. Перенос этого контракта не подтверждает работу с БД или конкурентные транзакции.
